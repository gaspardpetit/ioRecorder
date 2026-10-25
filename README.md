# ioRecorder

## Transcription with verbastream

ioRecorder can transcribe a live recording or an audio file played in the app. Install
`verbastream` separately, then open **Settings** and set the path to its executable
(or leave `verbastream` if it is on `PATH`). Choose English and/or French, a model,
and either low latency or quality mode. Turn on **Enable transcript** before starting
the recording or playback. The transcript appears in the app and is saved as a `.txt`
file in the selected output folder.

**Save audio** is optional. When enabled, the session audio is saved as WAV or MP3.
Diagnostic logging writes verbastream's output and error streams alongside the
session files. The transcript viewer uses the Microsoft Edge WebView2 Runtime.
