﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MainProject.Models;

namespace MainProject.Compresion
{
    public class LZW
    {
        Dictionary<long, string> alphabet = new Dictionary<long, string>();
        string allText;
        string previous; // w 
        string actual; // k        
        int cont = 1;
        List<long> output = new List<long>();


        public void GetText(StringBuilder text)
        {
            allText = text.ToString();
            //allText = allText.Replace("\r", "");
            //allText = allText.Remove(allText.Length - 1);
        }

        public void InitializeDictionary(byte[] text, string newName)
        {
            for (int i = 0; i < text.Length; i++)
            {
                string caracter = ByteGenerator.ConvertToString(new byte[] { text[i] });
                if (!alphabet.ContainsValue(caracter))
                {
                    alphabet.Add(cont, caracter);
                    cont++;
                }
            }

            string folder = string.Format(@"{0}Compressions\", AppContext.BaseDirectory.Substring(0, AppContext.BaseDirectory.IndexOf("bin")));
            string fullPath = folder + newName;
            DirectoryInfo directory = Directory.CreateDirectory(folder);

            string content = "";
            foreach (var item in alphabet)
            {
                KeyValuePair<long, string> pair = item;
                content += pair.Value;
            }

            using (FileStream writer = new FileStream(fullPath, FileMode.OpenOrCreate))
            {
                byte[] ToWrite = ByteGenerator.ConvertToBytes(content);
                for (int i = 0; i < ToWrite.Length; i++)
                {
                    byte[] temp = { ToWrite[i] };
                    writer.Seek(0, SeekOrigin.End);
                    writer.Write(temp, 0, 1);
                }
                writer.Seek(0, SeekOrigin.End);
                writer.Write(ByteGenerator.ConvertToBytes("@@@"), 0, 3);
            }
        }

        public void BuildLZW(byte[] text, string newName, string name)
        {
            string folder = string.Format(@"{0}Compressions\", AppContext.BaseDirectory.Substring(0, AppContext.BaseDirectory.IndexOf("bin")));
            string fullPath = folder + newName;
            DirectoryInfo directory = Directory.CreateDirectory(folder);
            previous = "";
            for (int i = 0; i < text.Length; i++)
            {
                actual = ByteGenerator.ConvertToString(new byte[] { text[i] });
                string aux = previous + actual;

                if (alphabet.ContainsValue(aux))
                {
                    previous += actual;
                }
                else
                {
                    // imprimir w
                    List<string> cadenas = alphabet.Values.ToList();
                    int output = cadenas.IndexOf(previous) + 1;
                    string binary = Convert.ToString(output, 2);
                    binary = binary.PadLeft(8, '0');
                    byte[] ToWrite = ConvertToByte(binary);

                    using (FileStream writer = new FileStream(fullPath, FileMode.Append))
                    {
                        writer.WriteByte(ToWrite[0]);
                    }

                    //agregar wk al diccionario
                    aux = previous + actual;
                    alphabet.Add(cont, aux);
                    cont++;

                    //w = k
                    previous = actual;
                }
            }

            //imprimir codigo de w 
            List<string> values = alphabet.Values.ToList();
            int w = values.IndexOf(previous) + 1;
            string wbinary = Convert.ToString(w, 2);
            wbinary.PadLeft(8, '0');
            byte[] Last = ConvertToByte(wbinary);

            using (FileStream writer = new FileStream(fullPath, FileMode.Append))
            {
                writer.WriteByte(Last[0]);
            }

            double compressedBytes = Last.Length;
            double originalBytes = text.Length;
            double rc = compressedBytes / originalBytes;
            double fc = originalBytes / compressedBytes;
            double percentage = rc * 100;

            CompressionsCollection newElement = new CompressionsCollection(name, fullPath, rc, fc, percentage.ToString("N2") + "%");
            DataCompressions.Instance.archivos.Insert(0, newElement);
        }
        public void Compress(byte[] text, string newName, string name)
        {
            previous = "";
            for (int i = 0; i < allText.Length; i++)
            {
                actual = allText[i].ToString();
                string aux = previous + actual;
                if (alphabet.Values.Contains(aux)) // if wk is on the dictionary
                {
                    previous = previous + actual;
                }
                else
                {
                    List<string> cadenas = alphabet.Values.ToList();
                    //agregar codigo de w
                    for (int j = 0; j < alphabet.Count; j++)
                    {
                        if (cadenas[j] == previous)
                        {
                            output.Add(alphabet.Keys.ToList()[j]);
                            j = alphabet.Count; //parar de comparar.
                        }
                    }

                    //agregar wk al diccionario 
                    cont++;
                    aux = previous + actual;
                    alphabet.Add(cont, aux);

                    //w = k
                    previous = actual;
                }
            }

            //imprimir codigo de w 
            List<string> values = alphabet.Values.ToList();
            for (int j = 0; j < alphabet.Count; j++)
            {
                if (values[j] == previous)
                {
                    output.Add(alphabet.Keys.ToList()[j]);
                    j = alphabet.Count; //parar de comparar.
                }
            }

            //OBTENCION DE CODIGOS DE SALIDA TERMINADA, imprimir en el archivo 

            string folder = string.Format(@"{0}Compressions\", AppContext.BaseDirectory.Substring(0, AppContext.BaseDirectory.IndexOf("bin")));
            string fullPath = folder + newName;

            // crear el directorio
            DirectoryInfo directory = Directory.CreateDirectory(folder);

            List<byte> allBytes = new List<byte>();

            for (int j = 0; j < output.Count; j++)
            {
                //byte[] ToWrite = Encoding.ASCII.GetBytes(output[j].ToString());                    
                string binary = Convert.ToString(output[j], 2);
                binary = binary.PadLeft(8, '0');
                byte[] sequence = binary.Select(c => Convert.ToByte(c.ToString())).ToArray();
                allBytes.AddRange(sequence);
            }

            string content = string.Join("", allBytes.ToArray());

            byte[] compressed = ConvertToByte(content);
            using (FileStream writer = new FileStream(fullPath, FileMode.OpenOrCreate))
            {
                byte[] ToWrite = ConvertToByte(content);
                for (int i = 0; i < ToWrite.Length; i++)
                {
                    byte[] temp = { ToWrite[i] };
                    writer.Seek(0, SeekOrigin.End);
                    writer.Write(temp, 0, 1);
                }
            }


            double compressedBytes = compressed.Length;
            double originalBytes = text.Length;
            double rc = compressedBytes / originalBytes;
            double fc = originalBytes / compressedBytes;
            double percentage = rc * 100;

            CompressionsCollection newElement = new CompressionsCollection(name, fullPath, rc, fc, percentage.ToString("N2") + "%");
            DataCompressions.Instance.archivos.Insert(0, newElement);

        }

        public void Decompress(byte[] txt, string name)
        {
            string content = ByteGenerator.ConvertToString(txt);
            string[] archivo = content.Split("@@@");
            byte[] originalDict = ByteGenerator.ConvertToBytes(archivo[0]);
            byte[] text = ByteGenerator.ConvertToBytes(archivo[1]);

            int TextPosition = originalDict.Length + 3;

            //contruir de nuevo el diccionario 
            for (int i = 0; i < originalDict.Length; i++)
            {
                alphabet.Add(cont, ByteGenerator.ConvertToString(new byte[] { originalDict[i] }));
                cont++;
            }

            //empezar a descifrar 
            //string outText = "";

            string single = "";
            int newCode = 0;
            byte[] oldByte = { txt[TextPosition] };
            TextPosition++;
            int oldCode = oldByte[0];
            //string caracter = ByteGenerator.ConvertToString(oldByte);
            string caracter = alphabet[oldCode];

            string folder = string.Format(@"{0}Compressions\", AppContext.BaseDirectory.Substring(0, AppContext.BaseDirectory.IndexOf("bin")));
            string fullpath = folder + name;
            DirectoryInfo directory = Directory.CreateDirectory(folder);

            using (FileStream writer = new FileStream(fullpath, FileMode.OpenOrCreate))
            {
                byte[] byt = ByteGenerator.ConvertToBytes(caracter);
                writer.Write(byt, 0, 1);
            }

            for (int i = TextPosition; i < txt.Length; i++)
            {
                byte[] byt = { txt[i] };
                newCode = byt[0];
                if (!alphabet.ContainsKey(newCode)) //si el codigo nuevo no está en el diccionario
                {
                    single = alphabet[oldByte[0]];
                    single += caracter;
                }
                else
                {
                    single = alphabet[byt[0]]; //ByteGenerator.ConvertToString(byt);
                }

                //outText += single;
                byte[] salida;
                using (FileStream writer = new FileStream(fullpath, FileMode.Append))
                {
                    salida = ByteGenerator.ConvertToBytes(single);
                    writer.Write(salida, 0, salida.Length);
                }

                caracter = single[0].ToString();
                alphabet.Add(cont, alphabet[oldByte[0]] + caracter);
                cont++;
                oldByte = byt;
            }

        }


        byte[] ConvertToByte(string b)
        {
            BitArray bits = new BitArray(b.Select(x => x == '1').ToArray());

            byte[] ret = ToByteArray(bits);

            return ret;
        }

        byte[] ToByteArray(BitArray bits)
        {
            int numBytes = bits.Count / 8;
            if (bits.Count % 8 != 0) numBytes++;

            byte[] bytes = new byte[numBytes];
            int byteIndex = 0, bitIndex = 0;

            for (int i = 0; i < bits.Count; i++)
            {
                if (bits[i])
                    bytes[byteIndex] |= (byte)(1 << (7 - bitIndex));

                bitIndex++;
                if (bitIndex == 8)
                {
                    bitIndex = 0;
                    byteIndex++;
                }
            }

            return bytes;
        }


        private BitArray ToBitArray(byte[] bytes)
        {
            string strAllbin = "";

            for (int i = 0; i < bytes.Length; i++)
            {
                byte byteindx = bytes[i];

                string strBin = Convert.ToString(byteindx, 2); // Convert from Byte to Bin
                strBin = strBin.PadLeft(8, '0');  // Zero Pad

                strAllbin += strBin;
            }

            BitArray ba = new BitArray(strAllbin.Select(x => x == '1').ToArray());
            return ba;
        }


    }
}
