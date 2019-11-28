using System.Collections.Generic;

namespace SecretSanta.Bot.Repository
{
    public interface IFileRepository
    {
        string GetFileContent();
        void SaveContentToFile(string content);
        void SaveMessagesToFile(List<SecretSantaEntry> messages);
        List<SecretSantaEntry> GetMessages();
        void SaveUserInfosToFile(List<UserSantaInfos> infos);
        List<UserSantaInfos> GetUserInfos();
        List<UserSantaInfos> GetUserSataInfosFromFile();
    }
}
