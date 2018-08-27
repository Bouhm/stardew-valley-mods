using PyTK;
using StardewValley;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PyTK.Types;

namespace NPCMapLocations.util
{
	class SerializationNpcList
	{
		public string Data { get; set; }

		public SerializationNpcList()
		{
			Data = null;
		}

		public SerializationNpcList(List<NPC> syncedNpcs)
		{
			MemoryStream stream = new MemoryStream();
			var serializer = new XmlSerializer(typeof(List<string>));
			using (stream)
				serializer.Serialize(stream, syncedNpcs);

			Data = PyNet.CompressBytes(stream.ToArray());
		}
	}
}
