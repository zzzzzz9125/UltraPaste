#if !Sony
using ScriptPortal.Vegas;
#else
using Sony.Vegas;
#endif

using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace UltraPaste
{
	public class UltraPasteCommandModule : ICustomCommandModule
	{
		private Vegas myVegas;

		public void InitializeModule(Vegas vegas)
		{
			myVegas = vegas;
		}

		public ICollection GetCustomCommands()
		{
            List<CustomCommand> customCommands = new List<CustomCommand>();
            new UltraPasteCommand().UltraPasteInit(myVegas, ref customCommands);

			return customCommands;
		}
	}
}