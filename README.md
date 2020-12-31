[logo]: https://raw.githubusercontent.com/Geeksltd/Zebble.Audio/master/icon.png "Zebble.Audio"


## Zebble.Audio

![logo]

A Zebble plugin to play or record audio files.


[![NuGet](https://img.shields.io/nuget/v/Zebble.Audio.svg?label=NuGet)](https://www.nuget.org/packages/Zebble.Audio/)

> This plugin enables you to record voice and save it to the device and play it in Android, UWP and iOS.

<br>


### Setup
* Available on NuGet: [https://www.nuget.org/packages/Zebble.Audio/](https://www.nuget.org/packages/Zebble.Audio/)
* Install in your platform client projects.
* Available for iOS, Android and UWP.
<br>


### Api Usage

If you have an audio file and you want to play it, you can just call `Device.Audio.Play()` and provide the relative path of the file or a stream URL, or If you want to use the microphone to record the user's voice or the surrounding sounds, you can call `Media.Audio.StartRecording()`.

##### Play Audio:
```csharp
//Play audio from device
Device.Audio.Play("MyFile.mp3");
//Play audio from URL
Device.Audio.Play("http://example.com/music/music.mp3");
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
| Play         | Task         | source -> string<br> errorAction -> OnError| x       | x   | x       |
| StopPlaying  | Task         | errorAction -> OnError| x       | x   | x       |
| StartRecording  | Task         | errorAction -> OnError| x       | x   | x       |
| StopRecording  | Task<byte[]>         | -| x       | x   | x       |