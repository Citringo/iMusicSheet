using System;
using System.Collections.Generic;
using System.Linq;
using AudioToolbox;
using AVFoundation;
using Foundation;
using static App1.MssfUtility;
using static App1.Logger;
using static App1.IOHelper;
using System.IO;

namespace App1
{


	public unsafe class MSSynth
	{
		private const int MaxChannelCount = 16;
		private const int MaxToneCount = 32;
		private const int numBuffers = 3;

		OutputAudioQueue queue;
		private double sampleRate;
		private NSError error;

		private Channel[] ch = new Channel[MaxChannelCount];
		private Tone?[] tones = new Tone?[MaxToneCount];
		private MSRenderer[] renderer = new MSRenderer[MaxToneCount];
		private Mssf[] mssfs = new Mssf[128];
		private double[] freqexts = new double[MaxChannelCount];

		private short[] rpns = new short[4];

		public void Reset()
		{
			for (int i = 0; i < ch.Length; i++)
			{
				ch[i] = new Channel
				{
					Panpot = 64,
					Volume = 100,
					Expression = 127,
					Pitchbend = 0,
					BendRange = 2,
					Inst = 0,
					NoteShift = 0,
					Tweak = 0
				};
			}

			for (int i = 0; i < renderer.Length; i++)
				renderer[i] = new MSRenderer();

			for (int i = 0; i < tones.Length; i++)
			{
				tones[i] = null;
			}

			SetFreqExtension();

		}

		public MSSynth()
		{

			Reset();
			

			//Mssf の取得
			for (int i = 0; i < 128; i++)
			{
				var path = GetFullBundlePath($"mssf/{i}.wav");
                if (!File.Exists(path))
                    continue;
				LoadFileDynamic(path);

			}

			AVAudioSession session = AVAudioSession.SharedInstance();
			session.SetCategory("AVAudioSessionCategoryPlayback", AVAudioSessionCategoryOptions.DefaultToSpeaker, out error);
			if (error != null)
			{
				Logger.Error(error);
			}
			sampleRate = session.SampleRate;
			var format = new AudioStreamBasicDescription
			{
				SampleRate = sampleRate,
				Format = AudioFormatType.LinearPCM,
				FormatFlags = AudioFormatFlags.LinearPCMIsPacked | AudioFormatFlags.LinearPCMIsSignedInteger,
				BitsPerChannel = 16,
				ChannelsPerFrame = 2,
				BytesPerFrame = 4,
				BytesPerPacket = 4,
				FramesPerPacket = 1
			};

			Logger.Info("set format. samplerate = " + sampleRate);
			queue = new OutputAudioQueue(format);
			Logger.Info("create queue");

			var bufferByteSize = (sampleRate > 16000) ? 2176 : 512; // 40.5 Hz : 31.25 Hz


			var buffers = new AudioQueueBuffer*[numBuffers];
			Logger.Info("create buffers");
			for (int i = 0; i < numBuffers; i++)
			{
				Logger.Info("Processing buffer {0}...", i + 1);
				queue.AllocateBuffer(bufferByteSize, out buffers[i]);
				Logger.Info("Allocated");
				GenerateTone(buffers[i]);
				Logger.Info("GenerateTone");
				queue.EnqueueBuffer(buffers[i], null);
				Logger.Info("Enqueue");
			}
			queue.BufferCompleted += (sender, e) =>
			{
				GenerateTone(e.UnsafeBuffer);
				//e.UnsafeBuffer->AudioDataByteSize = (uint)(numPacketsToRead * format.BytesPerPacket);
				queue.EnqueueBuffer(e.UnsafeBuffer, null);
			};
			Logger.Info("Set EventHandler");
			queue.Start();
			Logger.Info("queue start");
			
		}

		void SetFreqExtension()
		{
			for (int i = 0; i < MaxChannelCount; i++)
				SetFreqExtension(i);
		}


		void SetFreqExtension(int channel)
		{
			freqexts[channel] = Math.Pow(2, (ch[channel].Pitchbend / 8192.0) * (ch[channel].BendRange / 12.0)) * Math.Pow(2, (ch[channel].Tweak / 8192f) * (2 / 12f)) * Math.Pow(2, (ch[channel].NoteShift / 12f));

		}


		public void SendEvent(MidiNode command, int channel, byte[] data)
		{
			//Info("{0}ch.{1} {2}", channel, command, string.Join(", ", data));
			switch (command)
			{
				case MidiNode.NoteOff:
					if (tones.Count((tone) => tone != null) == 0)
						break;
					for (int i = 0; i < tones.Length; i++)
					{
						if (tones[i] == null)
							continue;
						var t = tones[i].GetValueOrDefault();
						if (t.NoteNum == data[0] && t.Channel == channel)
						{
							tones[i] = null;
						}
					}
					break;
				case MidiNode.NoteOn:
					if (channel == 9)
						break;
					if (data[1] == 0)	//NoteOnのベロシティが0の時はNoteOff同様になる
						goto case MidiNode.NoteOff;
					int? candidate = null;
					// すき間，またはダブっているToneを探す
					for (int i = 0; i < tones.Length; i++)
						if (tones[i] == null || (tones[i].Value.Channel == channel && tones[i].Value.NoteNum == data[0]))
						{
							candidate = i;
							break;
						}
					if (candidate == null)
					{
						//ここではtonesはすべてnullではないはず
						double max = 0;
						// 一番年上を卒業させる
						for (int i = 0; i < tones.Length; i++)
						{
							var t = tones[i].GetValueOrDefault();
							if (max < tick - t.Tick)
							{
								candidate = i;
								max = tick - t.Tick;
							}
						}
						if (candidate == null)
							break;	//それでもnullなら諦めよう
					}
					tones[candidate.Value] = new Tone(GetFreq(data[0]), data[0], data[1], channel, tick);
					break;
				case MidiNode.ControlChange:
					if (!Enum.IsDefined(typeof(ControlChangeType), (int)data[0]))
						break;
					var cc = (ControlChangeType)data[0];
					var value = data[1];
					switch (cc)
					{
						case ControlChangeType.Volume:
							ch[channel].Volume = value;
							break;
						case ControlChangeType.Panpot:
							ch[channel].Panpot = value;
							break;
						case ControlChangeType.Expression:
							ch[channel].Expression = value;
							break;
						case ControlChangeType.DataMSB:
							rpns[2] = value;
							switch (rpns[1])
							{
								case 0:
									ch[channel].BendRange = rpns[2];
									break;
								case 2:
									ch[channel].NoteShift = (short)(rpns[2] - 64);
									break;
							}
							break;
						case ControlChangeType.DataLSB:
							rpns[3] = value;
							if (rpns[1] == 1)
								ch[channel].Tweak = (short)((rpns[2] << 7) + rpns[3] - 8192);
							break;
						case ControlChangeType.RPNLSB:
							rpns[1] = value;
							break;
						case ControlChangeType.RPNMSB:
							rpns[0] = value;
							break;
						case ControlChangeType.AllSoundOff:
//							break;
						case ControlChangeType.ResetAllController:
//							break;
						case ControlChangeType.AllNoteOff:
//							break;
						case ControlChangeType.BankMSB:
//							break;
						case ControlChangeType.BankLSB:
//							break;
						case ControlChangeType.Mono:
//							break;
						case ControlChangeType.Poly:
//							break;
						case ControlChangeType.Modulation:
//							break;
						case ControlChangeType.HoldPedal:
//							break;
						case ControlChangeType.Reverb:
//							break;
						case ControlChangeType.Chorus:
//							break;
						case ControlChangeType.Delay:
							//Warning("NotImpl: {0}", cc);
							break;
						default:
							break;
					}
					break;
				case MidiNode.ProgramChange:
					ch[channel].Inst = data[0];
					break;
				case MidiNode.PitchBend:
					ch[channel].Pitchbend = ((data[1] << 7) + data[0]) - 8192;
					//Info("SetPitchBend: {0}", ch[channel].Pitchbend);
					SetFreqExtension(channel);
					break;
				default:
					break;
			}
		}


		double tick = 0;


		

		void GenerateTone(AudioQueueBuffer* buffer)
		{
			//Info("BufferCapacity = {0}", buffer->AudioDataBytesCapacity);
			uint sampleCount = buffer->AudioDataBytesCapacity / 4;
			//double bufferLength = sampleCount;
			//double wavelength = sampleRate / outputFrequency;
			//double repetitions = Math.Floor(bufferLength / wavelength);
			//if (repetitions > 0)
			//	sampleCount = (uint)Math.Round(wavelength * repetitions);
			double sd = 1.0 / sampleRate;
			double amp = 0.9;
			double max16bit = short.MaxValue;
			short* p = (short*)buffer->AudioData;
			double yl, yr;
			string log = "", blog = "";

			for (int i = 0; i < sampleCount; i++)
			{
				yl = 0; yr = 0;
				var cnt = tones.Count((tone) => tone != null);
				if (cnt > 0)
					for (int j = 0; j < tones.Length; j++)
					{
						if (tones[j] == null)
							continue;
						var t = tones[j].GetValueOrDefault();
						double freq = t.BaseFreq * freqexts[t.Channel];
						renderer[j].SetCycle(freq, sampleRate);
						/*if (t.Channel == 1)
						{
							blog = log;
							log = $"♪:{pitchnames[t.NoteNum % 12]}{t.NoteNum / 12 - 1} Freq:{(int)freq} BFreq:{(int)t.BaseFreq} Pitch:{ch[t.Channel].Pitchbend}\n";
							if (blog != log)
								LogAddToList(log);
                        }*/
						double x = renderer[j].Position;
						double panrt = ch[t.Channel].Panpot / 127.0;
						double volrt = ch[t.Channel].Volume / 127.0;
						double exprt = ch[t.Channel].Expression / 127.0;
						double velrt = t.Velocity / 127.0;
						double cntrt = 1d / tones.Length;

						yl += ((x % 1.0 < 0.5) ? 0.7 : -0.7) * (1 - panrt) * volrt * exprt * cntrt * velrt;
						yr += ((x % 1.0 < 0.5) ? 0.7 : -0.7) * panrt * volrt * exprt * cntrt * velrt;
					}
				p[i * 2 + 0] = (short)(yl * max16bit * amp);
				p[i * 2 + 1] = (short)(yr * max16bit * amp);
				
			}
			buffer->AudioDataByteSize = sampleCount * 4;
			tick++;
		}



		public static float GetRelativeFreq(int oct)
		{
			return GetFreq("A", oct);
		}


		public static float GetFreq(string pitch, int oct)
		{
			int hoge = oct * 12 + pitchnames.IndexOf(pitch) + 12;
			float hage = GetFreq(hoge);

			return hage;
		}

		public static List<string> pitchnames = new[] { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" }.ToList();


		public static float GetFreq(int noteno)
		{
			return (float)(441 * Math.Pow(2, (noteno - 69) / 12.0));
		}

	
	}

	public class MSRenderer
	{
		public MSRenderer()
		{
			step = 0;
			position = 0;
		}

		public MSRenderer(double cycle)
			: this()
		{
			SetCycle(cycle);
		}

		private double step;
		private double position;

		public void SetCycle(double cycle)
		{
			if (cycle > 0)
				step = Division / cycle;
			else
				step = 0;
		}

		const double Division = 1d;

		public void SetCycle(double freq, double samplerate)
		{
			SetCycle(samplerate / freq);
		}

		public double Position
		{
			get
			{
				return position = (position + step) % Division;
			}
		}

	}

}
