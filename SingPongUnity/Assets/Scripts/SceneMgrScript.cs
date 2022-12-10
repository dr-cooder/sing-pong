using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO.Ports; // For serial
using System.Threading;

public class SceneMgrScript : MonoBehaviour
{
    [Header("Scene objects")]
    public GameObject paddle;
    public GameObject ball;
    public GameObject wall;

    private float paddleX;
    private float paddleSpriteX;
    private float paddleY;
    private float paddleMaxY;
    private float paddleMinY;
    private float paddleForgivenessY;
    private float wallX;
    private float wallUpperY;
    private float wallLowerY;

    [Min(0)]
    public float ballMaxSpeedX;
    [Min(0)]
    public float ballMaxSpeedY;

    private float ballX;
    private float ballY;
    private float ballVelX;
    private float ballVelY;

    [Header("Arduino I/O")]
    public string sPort = "COM3";
    SerialPort stream; // From the Nano
    public int baud = 115200;
    public int readingsToSmooth = 72; // 1/4 second if monitor is 144Hz

    [Header("Input calibration")]
    public float magnitudeThreshold;
    public float minimumFrequency;
    public float maximumFrequency;

    private MicData micData;
    private string currentReading;

    class MicData
    {
        private int[] magnitudes;
        private double[] peakFreqs;
        private int magnitudeTotal;
        private double freqTotal;
        private int readingCount;
        private int index;

        public MicData(int readingCount, string initialReading)
        {
            this.readingCount = readingCount;
            index = 0;

            magnitudes = new int[readingCount];
            peakFreqs = new double[readingCount];

            (int initialMag, double initialPeak) = ParseReading(initialReading);

            for (int i = 0; i < readingCount; i++)
            {
                magnitudes[i] = initialMag;
                peakFreqs[i] = initialPeak;
            }

            magnitudeTotal = initialMag * readingCount;
            freqTotal = initialPeak * readingCount;
        }

        public void Read(string reading)
        {
            (int magnitude, double peakFreq) = ParseReading(reading);

            magnitudeTotal -= magnitudes[index];
            freqTotal -= peakFreqs[index];

            magnitudes[index] = magnitude;
            peakFreqs[index] = peakFreq;

            magnitudeTotal += magnitude;
            freqTotal += peakFreq;

            index++;
            while (index >= readingCount) index -= readingCount; 
        }

        private static (int magnitude, double peakFreq) ParseReading(string reading)
        {
            string[] readingSplit = reading.Split(',');
            int readingMag = int.Parse(readingSplit[0]);
            double readingPeak = double.Parse(readingSplit[1]);
            return (readingMag, readingPeak);
        }

        public double Magnitude => (double)magnitudeTotal / readingCount;
        public double PeakFreq => freqTotal / readingCount;
    }

    // Start is called before the first frame update
    void Start()
    {
        stream = new SerialPort(sPort, baud);
        stream.Open(); // Open serial stream
        currentReading = stream.ReadLine();
        micData = new MicData(readingsToSmooth, currentReading);
        new Thread(ArduinoThreadLoop).Start();

        float ballPaddingX = ball.transform.localScale.x / 2;
        float ballPaddingY = ball.transform.localScale.y / 2;

        paddleX = paddle.transform.position.x + paddle.transform.localScale.x / 2 + ballPaddingX;
        paddleSpriteX = paddle.transform.position.x;
        paddleY = wall.transform.position.y;
        paddleForgivenessY = paddle.transform.localScale.y / 2 + ballPaddingY;

        paddle.transform.position = new Vector2(paddleSpriteX, paddleY);

        wallX = wall.transform.position.x - wall.transform.localScale.x / 2 - ballPaddingX;
        wallUpperY = wall.transform.position.y + wall.transform.localScale.y / 2 - ballPaddingY;
        wallLowerY = wall.transform.position.y - wall.transform.localScale.y / 2 + ballPaddingY;

        paddleMaxY = (wall.transform.localScale.y - paddle.transform.localScale.y) / 2 + wall.transform.position.y;
        paddleMinY = -(wall.transform.localScale.y - paddle.transform.localScale.y) / 2 + wall.transform.position.y;

        ballX = paddleX;
        ballY = 0;
        ballVelX = ballMaxSpeedX;
        ballVelY = ballMaxSpeedY;

        ball.transform.position = new Vector2(ballX, ballY);
    }

    // Update is called once per frame
    void Update()
    {
        // Debug.Log(stream.ReadLine());
        micData.Read(currentReading);
        if (micData.Magnitude > 80)
        {
            // Lerp measured frequency to get respective paddle position
            paddleY =
                (paddleMaxY - paddleMinY) *
                Mathf.Clamp01((float)(
                    (micData.PeakFreq - minimumFrequency)
                    /
                    (maximumFrequency - minimumFrequency)
                ))
                + paddleMinY;
            paddle.transform.position = new Vector2(paddleSpriteX, paddleY);
        }

        float dt = Time.deltaTime;
        ballX += ballVelX * dt;
        ballY += ballVelY * dt;

        if (ballY >= wallUpperY && ballVelY > 0)
        {
            ballVelY = -Mathf.Abs(ballVelY);
            ballY = 2 * wallUpperY - ballY;
        }
        else if (ballY <= wallLowerY && ballVelY < 0)
        {
            ballVelY = Mathf.Abs(ballVelY);
            ballY = 2 * wallLowerY - ballY;
        }

        if (ballX >= wallX && ballVelX > 0)
        {
            ballVelX = -Mathf.Abs(ballVelX);
            ballX = 2 * wallX - ballX;
        }
        else if (ballX <= paddleX && ballVelX < 0)
        {
            // Check if paddle hit here
            ballVelX = Mathf.Abs(ballVelX);
            ballX = 2 * paddleX - ballX;
        }
        ball.transform.position = new Vector2(ballX, ballY);
    }

    void ArduinoThreadLoop ()
    {
        while (true)
        {
            currentReading = stream.ReadLine();
        }
    }
}
