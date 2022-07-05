using System.IO;
using System.Xml.Serialization;

namespace APKEasyTool
{
    public class XMLSave
    {
        public static void SaveData(object obj, string filename)
        {
            XmlSerializer sr = new XmlSerializer(obj.GetType());
            TextWriter writer = new StreamWriter (filename);
            sr.Serialize(writer, obj);
            writer.Close();
        }
    }
}
