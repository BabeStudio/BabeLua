using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;

namespace Babe.Lua.Editor
{
	class EncodingDecide
	{
		static int UTF8_Precent(byte[] buffer)
		{
			int pre = 0;
			int i, buflen = 0;
			int utf8_counts = 0, ascii_counts = 0;

			// Maybe also use UTF8 Byte Order Mark:  EF BB BF

			buflen = buffer.Length;
			for (i = 0; i < buflen; i++)
			{
				if ((buffer[i] & (byte)0x7F) == buffer[i])
				{  // One byte
					ascii_counts++;
				}
				else
				{
					int m_rawInt0 = Convert.ToInt16(buffer[i]);
					int m_rawInt1 = Convert.ToInt16(buffer[i + 1]);
					int m_rawInt2 = Convert.ToInt16(buffer[i + 2]);

					if (256 - 64 <= m_rawInt0 && m_rawInt0 <= 256 - 33 && // Two bytes
					 i + 1 < buflen &&
					 256 - 128 <= m_rawInt1 && m_rawInt1 <= 256 - 65)
					{
						utf8_counts += 2;
						i++;
					}
					else if (256 - 32 <= m_rawInt0 && m_rawInt0 <= 256 - 17 && // Three bytes
					 i + 2 < buflen &&
					 256 - 128 <= m_rawInt1 && m_rawInt1 <= 256 - 65 &&
					 256 - 128 <= m_rawInt2 && m_rawInt2 <= 256 - 65)
					{
						utf8_counts += 3;
						i += 2;
					}
				}
			}

			if (ascii_counts == buflen) { return 0; }

			pre = (int)(100 * ((float)utf8_counts / (float)(buflen - ascii_counts)));

			return pre;
		}

		public static EncodingName DecideBufferEncoding(byte[] buffer)
		{
			if (buffer.Length > 4 && buffer[0] == 0x00 && buffer[1] == 0x00)
			{
				if (buffer[2] == 0xFF && buffer[3] == 0xFE)
				{
					return EncodingName.UTF32_Little_Endian;
				}
				else if (buffer[2] == 0xFE && buffer[3] == 0xFF)
				{
					return EncodingName.UTF32_Big_Endian;
				}
				else
				{
					return EncodingName.ANSI;
				}
			}
			else if (buffer.Length > 3 && buffer[0] == 0xEF && buffer[1] == 0xBB && buffer[2] == 0xBF)
			{
				return EncodingName.UTF8_BOM;
			}
            else if (buffer.Length > 2)
            {
                if (buffer[0] == 0xFF && buffer[1] == 0xFE)
                {
                    return EncodingName.UTF16_Little_Endian;
                }
                else if (buffer[0] == 0xFE && buffer[1] == 0xFF)
                {
                    return EncodingName.UTF16_Big_Endian;
                }
                else
                {
                    int score = UTF8_Precent(buffer);

                    if (score != 100) return EncodingName.ANSI;
                    else
                    {
                        return EncodingName.UTF8;
                    }
                }
            }
            else
            {
                return EncodingName.ANSI;

            }
		}

		public static EncodingName DecideFileEncoding(string FileName)
		{
            var reader = File.OpenRead(FileName);
            
            byte[] buf = new byte[reader.Length];
            reader.Read(buf, 0, buf.Length);
            reader.Dispose();
            return DecideBufferEncoding(buf);
		}
	}

	public enum EncodingName
	{
		ANSI,
		UTF8,
		UTF8_BOM,
		UTF16_Little_Endian,
		UTF16_Big_Endian,
		UTF32_Little_Endian,
		UTF32_Big_Endian,
	}
}
