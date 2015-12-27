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
		private const int numPacketsToRead = 507;
		private NSError error;

		public Channel[] ch = new Channel[16];
		public Tone[] tones = new Tone[32];

		public MSSynth()
		{
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
			Logger.Info("set format");
			queue = new OutputAudioQueue(format);
			Logger.Info("create queue");
			var bufferByteSize = numPacketsToRead * format.BytesPerPacket;


			var buffers = new AudioQueueBuffer*[numBuffers];
			Logger.Info("create buffers");
			for (int i = 0; i < numBuffers; i++)
			{
				Logger.Info("Processing buffer {0}...", i);
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
				queue.EnqueueBuffer(e.UnsafeBuffer, null);
			};
			Logger.Info("Set EventHandler");
			queue.Start();
			Logger.Info("queue start");

		}

		public void SendEvent(MidiNode command, int channel, byte[] data)
		{
			Info("{0}ch.{1} {2}", channel, command, string.Join(", ", data));
			switch (command)
			{
				case MidiNode.NoteOff:
					if (tones.Count((tone) => tone != null) == 0)
						break;
					for (int i = 0; i < tones.Length; i++)
					{
						if (tones[i] == null)
							continue;
						if (tones[i].NoteNum == data[0] && tones[i].Channel == channel)
						{
							tones[i] = null;
							break;
						}
					}
					break;
				case MidiNode.NoteOn:
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
						int max = 0;
						// 一番年上を卒業させる
						for (int i = 0; i < tones.Length; i++)
							if (max < tones[i].Tick)
							{
								candidate = i;
								max = tones[i].Tick;
							}
						if (candidate == null)
							break;	//それでもnullなら諦めよう
					}
					tones[candidate.Value] = new Tone(GetFreq(data[0]), data[0], data[1], channel);
					break;
				case MidiNode.ControlChange:
					if (!Enum.IsDefined(typeof(ControlChangeType), data[0]))
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
					ch[channel].Pitchbend = data[1] << 7 + data[0];
					break;
				default:
					break;
			}
		}

		void GenerateTone(AudioQueueBuffer* buffer)
		{
			uint sampleCount = numPacketsToRead;
			//double bufferLength = sampleCount;
			//double wavelength = sampleRate / outputFrequency;
			//double repetitions = Math.Floor(bufferLength / wavelength);
			//if (repetitions > 0)
			//	sampleCount = (uint)Math.Round(wavelength * repetitions);
			double sd = 1.0 / sampleRate;
			double amp = 0.9;
			double max16bit = short.MaxValue;
			short* p = (short*)buffer->AudioData;
			for (int i = 0; i < sampleCount; i++)
			{
				double x = i * sd * 440;
				double yl, yr;
				yl = Math.Sin(x * 2.0 * Math.PI);
				yr = ((x % 1.0 < 0.5) ? 0.7 : -0.7);
				p[i * 2 + 0] = (short)(yl * max16bit * amp);
				p[i * 2 + 1] = (short)(yr * max16bit * amp);
			}
			buffer->AudioDataByteSize = sampleCount * 2;
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
