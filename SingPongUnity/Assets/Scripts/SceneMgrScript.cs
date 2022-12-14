using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO.Ports; // For serial
using System.Threading;
using TMPro;

public class SceneMgrScript : MonoBehaviour
{
    [Header("Scene objects")]
    public GameObject paddle;
    public GameObject ball;
    public GameObject wall;
    public TextMeshProUGUI scoreDisplay;

    private float paddleX;
    private float paddleSpriteX;
    private float paddleY;
    private float paddleDeltaY;
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
    [Range(0, 1)]
    public float ballWallRandSkewAmt;

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

    private int score = 0;
    private int highScore = 0;

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
        paddleDeltaY = 0;
        paddleForgivenessY = paddle.transform.localScale.y / 2 + ballPaddingY;

        paddle.transform.position = new Vector2(paddleSpriteX, paddleY);

        wallX = wall.transform.position.x - wall.transform.localScale.x / 2 - ballPaddingX;
        wallUpperY = wall.transform.position.y + wall.transform.localScale.y / 2 - ballPaddingY;
        wallLowerY = wall.transform.position.y - wall.transform.localScale.y / 2 + ballPaddingY;

        paddleMaxY = (wall.transform.localScale.y - paddle.transform.localScale.y) / 2 + wall.transform.position.y;
        paddleMinY = -(wall.transform.localScale.y - paddle.transform.localScale.y) / 2 + wall.transform.position.y;

        ResetBall();
    }

    // Update is called once per frame
    void Update()
    {
        micData.Read(currentReading);
        if (micData.Magnitude > magnitudeThreshold)
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
            paddleDeltaY = paddleY - paddle.transform.position.y;
            paddle.transform.position = new Vector2(paddleSpriteX, paddleY);
        }
        else
        {
            paddleDeltaY = 0;
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
            ballVelY = Mathf.Clamp(ballVelY + 2 * Random.Range(-ballMaxSpeedY, ballMaxSpeedY) * ballWallRandSkewAmt, -ballMaxSpeedY, ballMaxSpeedY);
            ballX = 2 * wallX - ballX;
        }
        else if (ballX <= paddleX && ballVelX < 0)
        {
            if (Mathf.Abs(ballY - paddleY) <= paddleForgivenessY)
            {
                score++;
                if (score > highScore) highScore = score;
                UpdateScoreDisplay();

                ballVelX = Mathf.Abs(ballVelX);
                ballVelY = Mathf.Clamp(ballVelY + paddleDeltaY, -ballMaxSpeedY, ballMaxSpeedY);
                ballX = 2 * paddleX - ballX;
            }
            else
            {
                ResetBall();
            }
        }
        ball.transform.position = new Vector2(ballX, ballY);
    }

    void UpdateScoreDisplay()
    {
        // https://learn.microsoft.com/en-us/dotnet/api/system.string.format
        scoreDisplay.text = $"SCORE BEST\n{string.Format("{0,5}{1,5}", score, highScore)}";
    }

    void ResetBall()
    {
        score = 0;
        UpdateScoreDisplay();

        ballX = paddleX;
        ballY = paddleY;
        ballVelX = ballMaxSpeedX;
        ballVelY = Random.Range(-ballMaxSpeedY, ballMaxSpeedY);

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
