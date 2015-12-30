using System;
using System.Collections.Generic;
using System.Linq;
using AudioToolbox;
using AVFoundation;
using Foundation;
using static App1.Utility;
using static App1.Logger;


namespace App1
{


	public unsafe class MSSynth
	{

		OutputAudioQueue queue;
		private double sampleRate;
		private const int numBuffers = 3;
		private const int numPacketsToRead = 1088;
		private NSError error;

		private Channel[] ch = new Channel[16];
		private Tone?[] tones = new Tone?[32];

		public MSSynth()
		{
			for (int i = 0; i < ch.Length; i++)
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
							break;
						}
					}
					break;
				case MidiNode.NoteOn:
					if (channel == 9)
						break;
					if (data[1] == 0)	//NoteOnのベロシティが0の時はNoteOff同様になる
						goto case MidiNode.NoteOff;
					int? candidate = null;
					// すき間を探す
					for (int i = 0; i < tones.Length; i++)
						if (tones[i] == null)
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
							if (max < t.Tick)
							{
								candidate = i;
								max = t.Tick;
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
//							break;
						case ControlChangeType.DataLSB:
//							break;
						case ControlChangeType.RPNLSB:
//							break;
						case ControlChangeType.RPNMSB:
//							break;
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
							Warning("NotImpl: {0}", cc);
							break;
						default:
							break;
					}
					break;
				case MidiNode.ProgramChange:
					ch[channel].Inst = data[0];
					break;
				case MidiNode.PitchBend:
					ch[channel].Pitchbend = (data[1] << 7 + data[0]) - 8192;
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
							double freq = t.BaseFreq * Math.Pow(2, (ch[t.Channel].Pitchbend / 8192.0) * (ch[t.Channel].BendRange / 12.0)) * Math.Pow(2, (ch[t.Channel].Tweak / 8192f) * (2 / 12f)) * Math.Pow(2, (ch[t.Channel].NoteShift / 12f));
							double x = tick * sd * freq;
							double panrt = ch[t.Channel].Panpot / 127.0;
							double volrt = ch[t.Channel].Volume / 127.0;
							double exprt = ch[t.Channel].Expression / 127.0;
							double cntrt = (double)cnt / tones.Length;

							yl += ((x % 1.0 < 0.5) ? 0.7 : -0.7) * (1 - panrt) * volrt * exprt * cntrt;
							yr += ((x % 1.0 < 0.5) ? 0.7 : -0.7) * panrt * volrt * exprt * cntrt;
					}
				p[i * 2 + 0] = (short)(yl * max16bit * amp);
				p[i * 2 + 1] = (short)(yr * max16bit * amp);
				tick++;
			}
			buffer->AudioDataByteSize = sampleCount * 4;
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
}
