using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SecretSanta.Bot.Repository
{
    public class SimpleJsonRepository : IFileRepository
    {
        private Dictionary<int, SecretSantaEntry> InMemory = new Dictionary<int, SecretSantaEntry>();
        private Dictionary<long, UserSantaInfos> UserSantaInfosMemory = new Dictionary<long, UserSantaInfos>();


        private string _fileName;
        private string _userInfosFileName;
        public SimpleJsonRepository(string filename, string userInfosFileName)
        {
            _fileName = filename;
            _userInfosFileName = userInfosFileName;
            GetMessagesFromFile().ForEach(x => InMemory.TryAdd(x.MessageId, x));
            GetUserSataInfosFromFile().ForEach(x => UserSantaInfosMemory.TryAdd(x.ChatId, x));

        }


        public string GetFileContent()
        {
            if (!File.Exists(_fileName))
            {
                var fs = File.Create(_fileName);
                fs.Close();
                return "";
            }

            return File.ReadAllText(_fileName);

        }

        private List<SecretSantaEntry> GetMessagesFromFile()
        {
            CreateFileIfNotExist(_fileName);

            var messagesJson = File.ReadAllText(_fileName);
            var loadedMessages = Newtonsoft.Json.JsonConvert.DeserializeObject<List<SecretSantaEntry>>(messagesJson);
            if (loadedMessages == null)
            {
                loadedMessages = new List<SecretSantaEntry>();
            }
            return loadedMessages;
        }

        public List<UserSantaInfos> GetUserSataInfosFromFile()
        {
            CreateFileIfNotExist(_userInfosFileName);

            var infosJson = File.ReadAllText(_userInfosFileName);
            var loadedInfos = Newtonsoft.Json.JsonConvert.DeserializeObject<List<UserSantaInfos>>(infosJson);
            if (loadedInfos == null)
            {
                loadedInfos = new List<UserSantaInfos>();
            }
            return loadedInfos;
        }

        public void SaveContentToFile(string content)
        {
            var existingContent = this.GetFileContent();
            var allContent = existingContent += content;
            File.WriteAllText(_fileName, content);
        }

        private void CreateFileIfNotExist(string filePath)
        {
            if (!File.Exists(filePath))
            {
                var fs = File.Create(filePath);
                fs.Close();
            }
        }

        public void SaveMessagesToFile(List<SecretSantaEntry> messages)
        {
            messages.ForEach(x => InMemory.TryAdd(x.MessageId, x));

            var formatting = Newtonsoft.Json.Formatting.Indented;
            var seriaizedTable = Newtonsoft.Json.JsonConvert.SerializeObject(GetMessages(), formatting);
            CreateFileIfNotExist(_fileName);
            File.WriteAllText(_fileName, seriaizedTable);
        }

        public List<SecretSantaEntry> GetMessages()
        {
            return InMemory.Select(x => x.Value).ToList();
        }

        public void SaveUserInfosToFile(List<UserSantaInfos> infos)
        {
            infos.ForEach(x => UserSantaInfosMemory.TryAdd(x.ChatId, x));
            infos.ForEach(x => UserSantaInfosMemory[x.ChatId] = x);

            var formatting = Newtonsoft.Json.Formatting.Indented;
            var seriaizedTable = Newtonsoft.Json.JsonConvert.SerializeObject(GetUserInfos(), formatting);
            CreateFileIfNotExist(_userInfosFileName);
            File.WriteAllText(_userInfosFileName, seriaizedTable);
        }

        public List<UserSantaInfos> GetUserInfos()
        {
            return UserSantaInfosMemory.Select(x => x.Value).ToList();
        }
    }
}
