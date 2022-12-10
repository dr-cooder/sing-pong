// arduinoFFT - Version: 1.5.6
#include <arduinoFFT.h>//use app Library Mgr to hunt and add this

arduinoFFT FFT = arduinoFFT(); /* Create FFT object */
/*
  These values can be changed in order to evaluate the functions
*/
#define CHANNEL A1
// int speakPin = 9;

const uint16_t samples = 64;//128; //64; //This value MUST ALWAYS be a power of 2
const double samplingFrequency = 9000; //Hz, must be less than 10000 due to ADC

unsigned int sampling_period_us;
unsigned long microseconds;

/*
  These are the input and output vectors
  Input vectors receive computed results from FFT
*/
double vReal[samples];
double vImag[samples];

#define SCL_INDEX 0x00
#define SCL_TIME 0x01
#define SCL_FREQUENCY 0x02
#define SCL_PLOT 0x03

void setup()
{
  sampling_period_us = round(1000000 * (1.0 / samplingFrequency));
  Serial.begin(115200);
  //Serial.println("Ready");
}

void loop()
{
  /*SAMPLING*/
  for (int i = 0; i < samples; i++)
  {
    microseconds = micros();    //Overflows after around 70 minutes!

    vReal[i] = analogRead(CHANNEL);
    vImag[i] = 0;
    while (micros() < (microseconds + sampling_period_us)) {
      //empty loop
    }
  }

  FFT.Windowing(vReal, samples, FFT_WIN_TYP_HAMMING, FFT_FORWARD);  /* Weigh data */
  FFT.Compute(vReal, vImag, samples, FFT_FORWARD); /* Compute FFT */
  FFT.ComplexToMagnitude(vReal, vImag, samples); /* Compute magnitudes */
  
  double peakFreq = FFT.MajorPeak(vReal, samples, samplingFrequency); /* peak freq? */
  
  //calculate a sort-of max magnitude from 1/4, 1/3, and 1/2 thru array of magnitudes
  long sortaMag = max(vReal[int(samples * .33)], vReal[int(samples * .5)]);
  sortaMag = max(sortaMag, vReal[int(samples * .25)]);
  Serial.print(sortaMag);
  Serial.print(",");
  
  Serial.println(peakFreq, 6); //Print out what frequency is the most dominant.
  
  // if (sortaMag > 300) { //If this sample was loud enough
  //   tone(speakPin, peakFreq, 800); //send out to speaker pin 
  //   delay(800); //wait for tone
  // }
  delay(2);
}
