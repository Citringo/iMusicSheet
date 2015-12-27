using System;

namespace App1
{
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

	
	public class Tone
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
		public int Tick
		{
			get;
			private set;
		}

		public int Channel { get; private set; }

		/// <summary>
		/// パラメーターを指定して、Tone の新しいインスタンスを初期化します。
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
			Tick = 0;
			NoteNum = n;
			Channel = c;
			EnvFlag = EnvelopeFlag.Attack;
		}

		/// <summary>
		/// 指定した Tone をコピーします。
		/// </summary>
		/// <param name="t">コピー元。</param>
		public Tone(Tone t)
		{
			BaseFreq = t.BaseFreq;
			Velocity = t.Velocity;
			Tick = t.Tick;
			NoteNum = t.NoteNum;
			Channel = t.Channel;
			EnvFlag = t.EnvFlag;
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
