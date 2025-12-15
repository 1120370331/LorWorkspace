using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SummonLiberation
{
	// Token: 0x02000006 RID: 6
	public class ButtonColor : EventTrigger
	{
		// Token: 0x06000012 RID: 18 RVA: 0x000026BB File Offset: 0x000008BB
		private void Update()
		{
		}

		// Token: 0x06000013 RID: 19 RVA: 0x000026BD File Offset: 0x000008BD
		public override void OnPointerEnter(PointerEventData eventData)
		{
			this.Image.color = ButtonColor.OnEnterColor;
		}

		// Token: 0x06000014 RID: 20 RVA: 0x000026CF File Offset: 0x000008CF
		public override void OnPointerExit(PointerEventData eventData)
		{
			this.Image.color = this.DefaultColor;
		}

		// Token: 0x06000015 RID: 21 RVA: 0x000026E2 File Offset: 0x000008E2
		public override void OnPointerUp(PointerEventData eventData)
		{
			this.Image.color = this.DefaultColor;
		}

		// Token: 0x04000006 RID: 6
		public Color DefaultColor = Color.white;

		// Token: 0x04000007 RID: 7
		public static Color OnEnterColor;

		// Token: 0x04000008 RID: 8
		public Image Image;
	}
}
