# Sing-Pong
## Premise
The gameplay is akin to that of Pong, albeit more like squash in that the player controls a paddle bounces a ball against an opposite wall as many times as they can before dropping it. This paddle is controlled by voice, hence the name of the project. Players are to sing into a wooden spoon with an attached microphone, the reading of which is to be interpreted by an Arduino setup as a pitch and sent to the Unity application via serial. The higher the pitch, the higher the paddle.
## Sources
- Read Arduino output asynchronously https://www.alanzucconi.com/2016/12/01/asynchronous-serial-communication/
- Format score with leading spaces https://learn.microsoft.com/en-us/dotnet/api/system.string.format
- Score font: Bitfont 5x3 by Matt LaGrandeur https://www.mattlag.com/bitfonts/
- In-class Arduino Mic FFT example
- In-class Arduino-Unity communication exercises