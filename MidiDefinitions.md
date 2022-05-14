
# Midi Definitions

## General Midi Instruments

Instrument          | Number
----------          | ------
AcousticGrandPiano  |  0
BrightAcousticPiano |  1
ElectricGrandPiano  |  2
HonkyTonkPiano      |  3
ElectricPiano1      |  4
ElectricPiano2      |  5
Harpsichord         |  6
Clavinet            |  7
Celesta             |  8
Glockenspiel        |  9
MusicBox            | 10
Vibraphone          | 11
Marimba             | 12
Xylophone           | 13
TubularBells        | 14
Dulcimer            | 15
DrawbarOrgan        | 16
PercussiveOrgan     | 17
RockOrgan           | 18
ChurchOrgan         | 19
ReedOrgan           | 20
Accordion           | 21
Harmonica           | 22
TangoAccordion      | 23
AcousticGuitarNylon | 24
AcousticGuitarSteel | 25
ElectricGuitarJazz  | 26
ElectricGuitarClean | 27
ElectricGuitarMuted | 28
OverdrivenGuitar    | 29
DistortionGuitar    | 30
GuitarHarmonics     | 31
AcousticBass        | 32
ElectricBassFinger  | 33
ElectricBassPick    | 34
FretlessBass        | 35
SlapBass1           | 36
SlapBass2           | 37
SynthBass1          | 38
SynthBass2          | 39
Violin              | 40
Viola               | 41
Cello               | 42
Contrabass          | 43
TremoloStrings      | 44
PizzicatoStrings    | 45
OrchestralHarp      | 46
Timpani             | 47
StringEnsemble1     | 48
StringEnsemble2     | 49
SynthStrings1       | 50
SynthStrings2       | 51
ChoirAahs           | 52
VoiceOohs           | 53
SynthVoice          | 54
OrchestraHit        | 55
Trumpet             | 56
Trombone            | 57
Tuba                | 58
MutedTrumpet        | 59
FrenchHorn          | 60
BrassSection        | 61
SynthBrass1         | 62
SynthBrass2         | 63
SopranoSax          | 64
AltoSax             | 65
TenorSax            | 66
BaritoneSax         | 67
Oboe                | 68
EnglishHorn         | 69
Bassoon             | 70
Clarinet            | 71
Piccolo             | 72
Flute               | 73
Recorder            | 74
PanFlute            | 75
BlownBottle         | 76
Shakuhachi          | 77
Whistle             | 78
Ocarina             | 79
Lead1Square         | 80
Lead2Sawtooth       | 81
Lead3Calliope       | 82
Lead4Chiff          | 83
Lead5Charang        | 84
Lead6Voice          | 85
Lead7Fifths         | 86
Lead8BassAndLead    | 87
Pad1NewAge          | 88
Pad2Warm            | 89
Pad3Polysynth       | 90
Pad4Choir           | 91
Pad5Bowed           | 92
Pad6Metallic        | 93
Pad7Halo            | 94
Pad8Sweep           | 95
Fx1Rain             | 96
Fx2Soundtrack       | 97
Fx3Crystal          | 98
Fx4Atmosphere       | 99
Fx5Brightness       | 100
Fx6Goblins          | 101
Fx7Echoes           | 102
Fx8SciFi            | 103
Sitar               | 104
Banjo               | 105
Shamisen            | 106
Koto                | 107
Kalimba             | 108
BagPipe             | 109
Fiddle              | 110
Shanai              | 111
TinkleBell          | 112
Agogo               | 113
SteelDrums          | 114
Woodblock           | 115
TaikoDrum           | 116
MelodicTom          | 117
SynthDrum           | 118
ReverseCymbal       | 119
GuitarFretNoise     | 120
BreathNoise         | 121
Seashore            | 122
BirdTweet           | 123
TelephoneRing       | 124
Helicopter          | 125
Applause            | 126
Gunshot             | 127

## General Midi Drums

Drum                | Number
----                | ------
AcousticBassDrum    | 35
BassDrum1           | 36
SideStick           | 37
AcousticSnare       | 38
HandClap            | 39
ElectricSnare       | 40
LowFloorTom         | 41
ClosedHiHat         | 42
HighFloorTom        | 43
PedalHiHat          | 44
LowTom              | 45
OpenHiHat           | 46
LowMidTom           | 47
HiMidTom            | 48
CrashCymbal1        | 49
HighTom             | 50
RideCymbal1         | 51
ChineseCymbal       | 52
RideBell            | 53
Tambourine          | 54
SplashCymbal        | 55
Cowbell             | 56
CrashCymbal2        | 57
Vibraslap           | 58
RideCymbal2         | 59
HiBongo             | 60
LowBongo            | 61
MuteHiConga         | 62
OpenHiConga         | 63
LowConga            | 64
HighTimbale         | 65
LowTimbale          | 66
HighAgogo           | 67
LowAgogo            | 68
Cabasa              | 69
Maracas             | 70
ShortWhistle        | 71
LongWhistle         | 72
ShortGuiro          | 73
LongGuiro           | 74
Claves              | 75
HiWoodBlock         | 76
LowWoodBlock        | 77
MuteCuica           | 78
OpenCuica           | 79
MuteTriangle        | 80
OpenTriangle        | 81

## Midi Controllers
- http://www.nortonmusic.com/midi_cc.html
- Undefined MIDI CCs: 3, 9, 14-15, 20-31, 85-90, 102-119
- For most controllers marked on/off, on=127 and off=0

Controller          | Number | Notes
----------          | ------ | -----
BankSelect          | 0      | MSB Followed by BankSelectLSB and Program Change
Modulation          | 1      |
BreathController    | 2      |
FootController      | 4      | MSB
PortamentoTime      | 5      | MSB Only use this for portamento time use 65 to turn on/off
Volume              | 7      | 7 and 11 both adjust the volume. Use 7 as your main control, set and forget
Balance             | 8      | MSB Some synths use it
Pan                 | 10     | MSB
Expression          | 11     | MSB See 7 - use 11 for volume changes during the track (crescendo, diminuendo, swells, etc.)
BankSelectLSB       | 32     | LSB
ModulationLSB       | 33     | LSB
BreathControllerLSB | 34     | LSB
FootControllerLSB   | 36     | LSB
PortamentoTimeLSB   | 37     | LSB
VolumeLSB           | 39     | LSB
BalanceLSB          | 40     | LSB
PanLSB              | 42     | LSB
ExpressionLSB       | 43     | LSB
Sustain             | 64     | Hold Pedal on/off
Portamento          | 65     | on/off
Sostenuto           | 66     | on/off
SoftPedal           | 67     | on/off
Legato              | 68     | on/off
Sustain2            | 69     | Hold Pedal 2 on/off
PortamentoControl   | 84     |
AllSoundOff         | 120    |
ResetAllControllers | 121    |
LocalKeyboard       | 122    |
AllNotesOff         | 123    |


## GM Drum Kits

Note that these will vary depending on your SF file.

Kit        | Number
-----------| ------
Standard   | 0
Room       | 8
Power      | 16
Electronic | 24
TR808      | 25
Jazz       | 32
Brush      | 40
Orchestra  | 48
SFX        | 56
