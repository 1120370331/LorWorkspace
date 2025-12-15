using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace BaseMod
{
	// Token: 0x0200005B RID: 91
	public class Shortcut
	{
		// Token: 0x060000CD RID: 205 RVA: 0x000073C2 File Offset: 0x000055C2
		public Shortcut()
		{
			this._link = NativeClasses.CreateShellLink();
		}

		// Token: 0x060000CE RID: 206 RVA: 0x000073D5 File Offset: 0x000055D5
		public Shortcut(string path) : this()
		{
			Marshal.ThrowExceptionForHR(this._link.SetPath(path));
		}

		// Token: 0x17000022 RID: 34
		// (get) Token: 0x060000CF RID: 207 RVA: 0x000073F0 File Offset: 0x000055F0
		// (set) Token: 0x060000D0 RID: 208 RVA: 0x00007435 File Offset: 0x00005635
		public string Path
		{
			get
			{
				NativeClasses._WIN32_FIND_DATAW win32_FIND_DATAW = default(NativeClasses._WIN32_FIND_DATAW);
				StringBuilder stringBuilder = new StringBuilder(512, 512);
				Marshal.ThrowExceptionForHR(this._link.GetPath(stringBuilder, stringBuilder.MaxCapacity, ref win32_FIND_DATAW, 2U));
				return stringBuilder.ToString();
			}
			set
			{
				Marshal.ThrowExceptionForHR(this._link.SetPath(value));
			}
		}

		// Token: 0x17000023 RID: 35
		// (get) Token: 0x060000D1 RID: 209 RVA: 0x00007448 File Offset: 0x00005648
		// (set) Token: 0x060000D2 RID: 210 RVA: 0x00007482 File Offset: 0x00005682
		public string Description
		{
			get
			{
				StringBuilder stringBuilder = new StringBuilder(512, 512);
				Marshal.ThrowExceptionForHR(this._link.GetDescription(stringBuilder, stringBuilder.MaxCapacity));
				return stringBuilder.ToString();
			}
			set
			{
				Marshal.ThrowExceptionForHR(this._link.SetDescription(value));
			}
		}

		// Token: 0x17000024 RID: 36
		// (set) Token: 0x060000D3 RID: 211 RVA: 0x00007495 File Offset: 0x00005695
		public string RelativePath
		{
			set
			{
				Marshal.ThrowExceptionForHR(this._link.SetRelativePath(value, 0U));
			}
		}

		// Token: 0x17000025 RID: 37
		// (get) Token: 0x060000D4 RID: 212 RVA: 0x000074AC File Offset: 0x000056AC
		// (set) Token: 0x060000D5 RID: 213 RVA: 0x000074E6 File Offset: 0x000056E6
		public string WorkingDirectory
		{
			get
			{
				StringBuilder stringBuilder = new StringBuilder(512, 512);
				Marshal.ThrowExceptionForHR(this._link.GetWorkingDirectory(stringBuilder, stringBuilder.MaxCapacity));
				return stringBuilder.ToString();
			}
			set
			{
				Marshal.ThrowExceptionForHR(this._link.SetWorkingDirectory(value));
			}
		}

		// Token: 0x17000026 RID: 38
		// (get) Token: 0x060000D6 RID: 214 RVA: 0x000074FC File Offset: 0x000056FC
		// (set) Token: 0x060000D7 RID: 215 RVA: 0x00007536 File Offset: 0x00005736
		public string Arguments
		{
			get
			{
				StringBuilder stringBuilder = new StringBuilder(512, 512);
				Marshal.ThrowExceptionForHR(this._link.GetArguments(stringBuilder, stringBuilder.MaxCapacity));
				return stringBuilder.ToString();
			}
			set
			{
				Marshal.ThrowExceptionForHR(this._link.SetArguments(value));
			}
		}

		// Token: 0x17000027 RID: 39
		// (get) Token: 0x060000D8 RID: 216 RVA: 0x0000754C File Offset: 0x0000574C
		// (set) Token: 0x060000D9 RID: 217 RVA: 0x0000756C File Offset: 0x0000576C
		public ushort HotKey
		{
			get
			{
				ushort result;
				Marshal.ThrowExceptionForHR(this._link.GetHotkey(out result));
				return result;
			}
			set
			{
				Marshal.ThrowExceptionForHR(this._link.SetHotkey(value));
			}
		}

		// Token: 0x060000DA RID: 218 RVA: 0x0000757F File Offset: 0x0000577F
		public void Resolve(IntPtr hwnd, uint flags)
		{
			Marshal.ThrowExceptionForHR(this._link.Resolve(hwnd, flags));
		}

		// Token: 0x060000DB RID: 219 RVA: 0x00007593 File Offset: 0x00005793
		public void Resolve(IWin32Window window)
		{
			this.Resolve(window.Handle, 0U);
		}

		// Token: 0x060000DC RID: 220 RVA: 0x000075A2 File Offset: 0x000057A2
		public void Resolve()
		{
			this.Resolve(IntPtr.Zero, 1U);
		}

		// Token: 0x17000028 RID: 40
		// (get) Token: 0x060000DD RID: 221 RVA: 0x000075B0 File Offset: 0x000057B0
		private NativeClasses.IPersistFile AsPersist
		{
			get
			{
				return (NativeClasses.IPersistFile)this._link;
			}
		}

		// Token: 0x060000DE RID: 222 RVA: 0x000075BD File Offset: 0x000057BD
		public void Save(string fileName)
		{
			Marshal.ThrowExceptionForHR(this.AsPersist.Save(fileName, true));
		}

		// Token: 0x060000DF RID: 223 RVA: 0x000075D1 File Offset: 0x000057D1
		public void Load(string fileName)
		{
			Marshal.ThrowExceptionForHR(this.AsPersist.Load(fileName, 0U));
		}

		// Token: 0x04000118 RID: 280
		private const int MAX_DESCRIPTION_LENGTH = 512;

		// Token: 0x04000119 RID: 281
		private const int MAX_PATH = 512;

		// Token: 0x0400011A RID: 282
		private NativeClasses.IShellLinkW _link;
	}
}
