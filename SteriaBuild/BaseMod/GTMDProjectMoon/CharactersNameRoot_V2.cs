using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace GTMDProjectMoon
{
	// Token: 0x0200000A RID: 10
	[XmlType("CharactersNameRoot")]
	public class CharactersNameRoot_V2 : XmlRoot
	{
		// Token: 0x04000010 RID: 16
		[XmlElement("Name")]
		public List<CharacterName_V2> nameList;
	}
}
