[logo]: https://raw.githubusercontent.com/Geeksltd/Zebble.Audio/master/Shared/NuGet/Icon.png "Zebble.Audio"


## Zebble.Audio

![logo]

A Zebble plugin to play or record an audio file.


[![NuGet](https://img.shields.io/nuget/v/Zebble.Audio.svg?label=NuGet)](https://www.nuget.org/packages/Zebble.Audio/)

> This plugin makes you able to record voice and save it to the device and play it, in Android, UWP, and IOS.

<br>


### Setup
* Available on NuGet: [https://www.nuget.org/packages/Zebble.Audio/](https://www.nuget.org/packages/Zebble.Audio/)
* Install in your platform client projects.
* Available for iOS, Android and UWP.
<br>


### Api Usage

If you have an audio file and you want to play it, you can just call `Device.Audio.Play()` and provide the relative path of the file, or, If you want to use the microphone to record the user's voice or the surrounding sounds, you can call `Media.Audio.StartRecording()`.

##### Play Audio:
```csharp
Device.Audio.Play("MyFile.mp3");
```
##### Record Audio:
```csharp
Device.Audio.StartRecording();
byte[] audiodata = await Device.Audio.StopRecording();
```
<br>


### Methods
| Method       | Return Type  | Parameters                          | Android | iOS | Windows |
| :----------- | :----------- | :-----------                        | :------ | :-- | :------ |
| Play         | Task         | file -> string<br> errorAction -> OnError| x       | x   | x       |
| StopPlaying  | Task         | errorAction -> OnError| x       | x   | x       |
| StartRecording  | Task         | errorAction -> OnError| x       | x   | x       |
| StopRecording  | Task         | -| x       | x   | x       |