using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using CoreMidi;
using AudioToolbox;

using Foundation;
using System.Threading;

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

				
			}
		}

		public async Task PlayAsync(string fileurl, CancellationToken ct)
		{

			virtualMidi = new MidiClient("VirtualClient");
			virtualMidi.IOError += (object sender, IOErrorEventArgs e) => {
				Logger.Warning("IO Error, messageId={0}", e.ErrorCode);
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



	

}
