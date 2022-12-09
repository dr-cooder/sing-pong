using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO.Ports; // For serial

public class SceneMgrScript : MonoBehaviour
{
    [Header("Arduino I/O")]
    public string sPort = "COM3";
    SerialPort stream; // From the Nano
    public int baud = 115200;
    public int readingsToSmooth = 36; // 1/4 second if monitor is 144Hz

    private MicData micData;

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
        micData = new MicData(readingsToSmooth, stream.ReadLine());
    }

    // Update is called once per frame
    void Update()
    {
        // Debug.Log(stream.ReadLine());
        micData.Read(stream.ReadLine());
        Debug.Log(micData.PeakFreq);
        // if (micData.Magnitude > 50) Debug.Log(micData.PeakFreq);
    }
}
