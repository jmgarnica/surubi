using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TigerCs.CompilationServices
{
	public class GuidGenerator
	{
		byte[] guid;

		public GuidGenerator()
		{
			guid = new byte[16];
		}

		public Guid GNext()
		{
			lock (guid)
			{
				int i = 15;

				while (i >= 0)
				{
					byte actual = guid[i];
					checked
					{
						try
						{
							actual++;
							guid[i] = actual;
							return new Guid(guid);
						}
						catch (ArithmeticException)
						{
							guid[i] = 0;
							i--;
						}
					}
				}
				throw new InvalidOperationException();
			}
		}
	}
}
