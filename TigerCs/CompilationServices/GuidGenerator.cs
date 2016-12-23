using System;

namespace TigerCs.CompilationServices
{
	public class GuidGenerator
	{
		readonly byte[] guid;

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
