using System;
using System.IO;

namespace BaseMod
{
	// Token: 0x02000064 RID: 100
	public class WAV
	{
		// Token: 0x0600024D RID: 589 RVA: 0x0001A157 File Offset: 0x00018357
		private static float bytesToFloat(byte firstByte, byte secondByte)
		{
			return (float)((short)((int)secondByte << 8 | (int)firstByte)) / 32768f;
		}

		// Token: 0x0600024E RID: 590 RVA: 0x0001A168 File Offset: 0x00018368
		private static int bytesToInt(byte[] bytes, int offset = 0)
		{
			int num = 0;
			for (int i = 0; i < 4; i++)
			{
				num |= (int)bytes[offset + i] << i * 8;
			}
			return num;
		}

		// Token: 0x0600024F RID: 591 RVA: 0x0001A193 File Offset: 0x00018393
		private static byte[] GetBytes(string filename)
		{
			return File.ReadAllBytes(filename);
		}

		// Token: 0x1700002F RID: 47
		// (get) Token: 0x06000250 RID: 592 RVA: 0x0001A19B File Offset: 0x0001839B
		// (set) Token: 0x06000251 RID: 593 RVA: 0x0001A1A3 File Offset: 0x000183A3
		public float[] LeftChannel { get; internal set; }

		// Token: 0x17000030 RID: 48
		// (get) Token: 0x06000252 RID: 594 RVA: 0x0001A1AC File Offset: 0x000183AC
		// (set) Token: 0x06000253 RID: 595 RVA: 0x0001A1B4 File Offset: 0x000183B4
		public float[] RightChannel { get; internal set; }

		// Token: 0x17000031 RID: 49
		// (get) Token: 0x06000254 RID: 596 RVA: 0x0001A1BD File Offset: 0x000183BD
		// (set) Token: 0x06000255 RID: 597 RVA: 0x0001A1C5 File Offset: 0x000183C5
		public int ChannelCount { get; internal set; }

		// Token: 0x17000032 RID: 50
		// (get) Token: 0x06000256 RID: 598 RVA: 0x0001A1CE File Offset: 0x000183CE
		// (set) Token: 0x06000257 RID: 599 RVA: 0x0001A1D6 File Offset: 0x000183D6
		public int SampleCount { get; internal set; }

		// Token: 0x17000033 RID: 51
		// (get) Token: 0x06000258 RID: 600 RVA: 0x0001A1DF File Offset: 0x000183DF
		// (set) Token: 0x06000259 RID: 601 RVA: 0x0001A1E7 File Offset: 0x000183E7
		public int Frequency { get; internal set; }

		// Token: 0x0600025A RID: 602 RVA: 0x0001A1F0 File Offset: 0x000183F0
		public WAV(string filename) : this(WAV.GetBytes(filename))
		{
		}

		// Token: 0x0600025B RID: 603 RVA: 0x0001A200 File Offset: 0x00018400
		public WAV(byte[] wav)
		{
			this.ChannelCount = (int)wav[22];
			this.Frequency = WAV.bytesToInt(wav, 24);
			int i = 12;
			while (wav[i] != 100 || wav[i + 1] != 97 || wav[i + 2] != 116 || wav[i + 3] != 97)
			{
				i += 4;
				int num = (int)wav[i] + (int)wav[i + 1] * 256 + (int)wav[i + 2] * 65536 + (int)wav[i + 3] * 16777216;
				i += 4 + num;
			}
			i += 8;
			this.SampleCount = (wav.Length - i) / 2;
			if (this.ChannelCount == 2)
			{
				this.SampleCount /= 2;
			}
			this.LeftChannel = new float[this.SampleCount];
			if (this.ChannelCount == 2)
			{
				this.RightChannel = new float[this.SampleCount];
			}
			else
			{
				this.RightChannel = null;
			}
			int num2 = 0;
			while (i < wav.Length)
			{
				this.LeftChannel[num2] = WAV.bytesToFloat(wav[i], wav[i + 1]);
				i += 2;
				if (this.ChannelCount == 2)
				{
					this.RightChannel[num2] = WAV.bytesToFloat(wav[i], wav[i + 1]);
					i += 2;
				}
				num2++;
			}
		}

		// Token: 0x0600025C RID: 604 RVA: 0x0001A330 File Offset: 0x00018530
		public override string ToString()
		{
			return string.Format("[WAV: LeftChannel={0}, RightChannel={1}, ChannelCount={2}, SampleCount={3}, Frequency={4}]", new object[]
			{
				this.LeftChannel,
				this.RightChannel,
				this.ChannelCount,
				this.SampleCount,
				this.Frequency
			});
		}
	}
}
