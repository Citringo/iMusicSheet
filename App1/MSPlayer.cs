using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using AudioUnit;
using CoreMidi;
using AudioToolbox;

using Foundation;
using CoreFoundation;
using System.Threading;
using AVFoundation;

namespace App1
{
	public class MSPlayer
	{

		MidiClient virtualMidi;
		MidiEndpoint virtualEndpoint;
		private MSSynth synth;

		void MidiMessageReceived(object sender, MidiPacketsEventArgs midiPacketArgs)
		{
			var packets = midiPacketArgs.Packets;

			for (int i = 0; i < packets.Length; i++)
			{
				var packet = packets[i];
				byte[] data = new byte[packet.Length];
				Marshal.Copy(packet.Bytes, data, 0, packet.Length);
				var midiStatus = data[0];
				var midiCommand = midiStatus >> 4;
				var midiChannel = midiStatus & 0x0F;

				for (int j = 1; j < data.Length; j++)
					data[j] &= 0x7F;

				switch ((MidiNode)midiCommand)
				{
					case MidiNode.NoteOn:
						//samplerUnit.MusicDeviceMIDIEvent((uint)midiStatus, (uint)note, (uint)velocity);
						if (data[2] > 0)
							Console.WriteLine("{0}CH NOTEON {1} {2}", midiChannel, data[1], data[2]);
						else
							Console.WriteLine("{0}CH NOTEOFF {1} {2}", midiChannel, data[1], data[2]);
						break;
					case MidiNode.NoteOff:
						Console.WriteLine("{0}CH NOTEOFF {1} {2}", midiChannel, data[1], data[2]);
						break;
					case MidiNode.ProgramChange:

						break;
					case MidiNode.PitchBend:

						break;
					case MidiNode.ControlChange:

						break;
				}
			}
		}

		public async Task PlayAsync(string fileurl, CancellationToken ct)
		{

			virtualMidi = new MidiClient("VirtualClient");
			virtualMidi.IOError += (object sender, IOErrorEventArgs e) => {
				Logger.Error("IO Error, messageId={0}", e.ErrorCode);
			};

			virtualMidi.PropertyChanged += (object sender, ObjectPropertyChangedEventArgs e) => {
				Logger.Info("Property changed: " + e.MidiObject + ", " + e.PropertyName);
			};

			MidiError error;
			virtualEndpoint = virtualMidi.CreateVirtualDestination("Virtual Destination", out error);

			if (error != MidiError.Ok)
				throw new Exception("Error creating virtual destination: " + error);
			virtualEndpoint.MessageReceived += MidiMessageReceived;

			var sequence = new MusicSequence();

			sequence.LoadFile(NSUrl.FromFilename(fileurl), MusicSequenceFileTypeID.Midi);

			var player = new MusicPlayer();

			sequence.SetMidiEndpoint(virtualEndpoint);

			player.MusicSequence = sequence;
			player.Preroll();
			player.Start();

			MusicTrack track;
			track = sequence.GetTrack(1);
			var length = track.TrackLength;
			while (true)
			{
				try
				{
					await Task.Delay(3, ct);
				}
				catch (OperationCanceledException)
				{
					player.Stop();
					sequence.Dispose();
					player.Dispose();
					Logger.Info("Midi playing is canceled.");
					throw;
				}
				double now = player.Time;
				OnLoop(new LoopEventArgs(now, length));
				if (now > length)
					break;
			}

			player.Stop();
			sequence.Dispose();
			player.Dispose();
			Logger.Info("Midi playing is successfully finished!");
		}

		public MSPlayer()
		{
			synth = new MSSynth();
		}

		public event LoopEventHandler Loop;

		protected virtual void OnLoop(LoopEventArgs e)
		{
			if (Loop != null)
				Loop(this, e);
		}

	}




	public delegate void LoopEventHandler(object sender, LoopEventArgs e);

	public class LoopEventArgs : EventArgs
	{
		private readonly double nowTime;
		private readonly double length;

		public LoopEventArgs(double t, double l)
		{
			nowTime = t;
			length = l;
		}

		public double NowTime => nowTime;

		public double Length => length;



	}

	public enum MidiNode
	{
		NoteOff = 0x8,
		NoteOn = 0x9,
		ControlChange = 0xB,
		ProgramChange = 0xC,
		PitchBend = 0xE
	}

	public enum ControlChangeType
	{
		BankMSB,
		Modulation,
		DataMSB = 6,
		Volume,
		Panpot = 10,
		Expression,
		BankLSB = 32,
		DataLSB = 38,
		HoldPedal = 64,
		Reverb = 91,
		Chorus = 93,
		Delay,
		RPNLSB = 100,
		RPNMSB,
		AllSoundOff = 120,
		ResetAllController,
		AllNoteOff = 123,
		Mono = 126,
		Poly
	}

	public unsafe class MSSynth
	{
		private enum WaveForm
		{
			Sine,
			Triangle,
			Sawtooth,
			Square
		}
		OutputAudioQueue queue;
		private double sampleRate;
		private const int numBuffers = 3;
		private bool alternate = true;
		private NSError error;
		private const float outputFrequency = 440;
		private WaveForm outputWaveForm;
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
			var bufferByteSize = (sampleRate > 16000.0) ? 2176 : 512;


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
				
				if (alternate)
				{
					outputWaveForm++;
					if (outputWaveForm > WaveForm.Square)
					{
						outputWaveForm = WaveForm.Sine;
					}
					GenerateTone(e.UnsafeBuffer);
				}
				queue.EnqueueBuffer(e.UnsafeBuffer, null);
			};
			Logger.Info("Set EventHandler");
			queue.Start();
			Logger.Info("queue start");

		}

		void GenerateTone(AudioQueueBuffer* buffer)
		{
			uint sampleCount = buffer->AudioDataBytesCapacity / 2;
			double bufferLength = sampleCount;
			double wavelength = sampleRate / outputFrequency;
			double repetitions = Math.Floor(bufferLength / wavelength);
			if (repetitions > 0)
				sampleCount = (uint)Math.Round(wavelength * repetitions);
			double sd = 1.0 / sampleRate;
			double amp = 0.9;
			double max16bit = short.MaxValue;
			short* p = (short*)buffer->AudioData;
			for (int i = 0; i < sampleCount; i++)
			{
				double x = i * sd * outputFrequency;
				double yl, yr;
				yl = Math.Sin(x * 2.0 * Math.PI);
				yr = ((x % 1.0 < 0.5) ? 0.7 : -0.7);
				p[i * 2 + 0] = (short)(yl * max16bit * amp);
				p[i * 2 + 1] = (short)(yr * max16bit * amp);
			}
			buffer->AudioDataByteSize = sampleCount * 2;
		}
	}

	public struct Channel : IChannel
	{
		public byte Inst
		{
			get;
			set;
		}
		public byte Panpot
		{
			get;
			set;
		}
		public byte Volume
		{
			get;
			set;
		}
		public byte Expression
		{
			get;
			set;
		}
		public int Pitchbend
		{
			get;
			set;
		}
		public short Tweak
		{
			get;
			set;
		}
		public short NoteShift
		{
			get;
			set;
		}
		public short BendRange
		{
			get;
			set;
		}
		public Channel(byte i, byte p, byte v, byte e, short t, short n, short b)
		{
			Inst = i;
			Panpot = p;
			Volume = v;
			Expression = e;
			Tweak = t;
			NoteShift = n;
			BendRange = b;
			Pitchbend = 0;
		}
	}

	public interface IChannel
	{

		short BendRange
		{
			get;
			set;
		}
		byte Expression
		{
			get;
			set;
		}
		byte Inst
		{
			get;
			set;
		}
		short NoteShift
		{
			get;
			set;
		}
		byte Panpot
		{
			get;
			set;
		}
		int Pitchbend
		{
			get;
			set;
		}
		short Tweak
		{
			get;
			set;
		}
		byte Volume
		{
			get;
			set;
		}
	}

	public struct Envelope
	{
		public int AttackTime
		{
			get;
			set;
		}
		public int DecayTime
		{
			get;
			set;
		}
		public byte SustainLevel
		{
			get;
			set;
		}
		public int ReleaseTime
		{
			get;
			set;
		}
		public Envelope(int a, int d, byte s, int r)
		{
			AttackTime = a;
			DecayTime = d;
			SustainLevel = s;
			ReleaseTime = r;
		}
	}

	public enum EnvelopeFlag
	{
		None,
		Attack,
		Decay,
		Sustain,
		Release
	}

	public struct Tone
	{
		public float BaseFreq
		{
			get;
			private set;
		}
		public int NoteNum
		{
			get;
			private set;
		}
		public EnvelopeFlag EnvFlag
		{
			get;
			private set;
		}
		public int Velocity
		{
			get;
			private set;
		}
		public long TimeStamp
		{
			get;
			private set;
		}

		public int Channel { get; set; }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="f"></param>
		/// <param name="n"></param>
		/// <param name="e"></param>
		/// <param name="v"></param>
		/// <param name="c"></param>
		public Tone(float f, int n, int v, int c)
		{
			BaseFreq = f;
			Velocity = v;
			TimeStamp = DateTime.Now.Ticks;
			NoteNum = n;
			Channel = c;
			EnvFlag = EnvelopeFlag.Attack;
		}
	}
	public enum NoiseOption
	{
		None,
		Long,
		Short
	}
	public struct Mssf
	{
		public static readonly Mssf Empty = default(Mssf);
		public short[] Wave
		{
			get;
			private set;
		}
		public int Pan
		{
			get;
			private set;
		}
		public Envelope Envelope
		{
			get;
			private set;
		}
		public NoiseOption Noise
		{
			get;
			private set;
		}
		public Mssf(short[] w, int a, int d, byte s, int r, int pan)
		{
			this.Wave = w;
			this.Envelope = new Envelope
			{
				AttackTime = a,
				DecayTime = d,
				SustainLevel = s,
				ReleaseTime = r
			};
			this.Pan = pan;
			this.Noise = NoiseOption.None;
		}
		public Mssf(short[] w, int a, int d, byte s, int r, int pan, NoiseOption noise)
		{
			this = new Mssf(w, a, d, s, r, pan);
			this.Noise = noise;
		}
	}

}
