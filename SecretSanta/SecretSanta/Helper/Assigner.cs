using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SecretSanta.Data;

namespace SecretSanta.Helper
{
    public class Assigner
    {
        private Repository _repository;
        private readonly ILogger<Assigner> _logger;
        private const string SHOW_INFO = "Покажи всю информацию еще раз";
        private const string SINGN_OUT = "Не хочу участвовать";
        public Assigner(Repository repository, ILogger<Assigner> logger)
        {
            _repository = repository;
            this._logger = logger;
        }


        /// <summary>
        /// Найти всех пользователей с заполненными адресами - это санты без внучков 
        /// Создать пустой список для сант с их внучками(матчингами)
        /// посчитать количество всех потенциальных сант
        /// найти первого случайного санту, уменьшить количество потенциальных сант на одного и убрать найденного из этого списка
        /// продолжать пока список сант не опустеет:
        ///     Если случайный санта может слать только по россии:
        ///         выбрать случайный индекс из списка сант без внучков, которые находятся в россии
        ///         и выбрать случайного внучка по этому индексу
        ///     иначе
        ///         выбрать случайный индекс из списка сант без внучков, вне зависимости от места положения
        ///         и выбрать случайного внучка по этому индексу
        ///     Установить случайному санте выбранного случайного внучка и наоборот, внучку санту(залинковать по userID)
        ///     добавить санту в список для сант с внучками (матчингами)
        ///     
        ///     Теперь выбраный внучок сам становится сантой для следующего цикла
        ///     уменьшаем количество всех незаматченных сант на 1
        ///     удаляем нового санту из списка сант без внучков
        /// конец цикла
        /// Установить случайному санте выбранного случайного внучка и наоборот, внучку санту(залинковать по userID)
        /// добавить санту в список для сант с внучками (матчингами)
        /// сохраняем список сант с внучками в базу
        /// </summary>
        /// <returns></returns>
        public List<(string Santa, string Grandson)> TriggerMatching(Guid groupId)
        {

            var currentChat = _repository.GetByGroupId(groupId);



            _logger.LogDebug("Prepare to match santas in chat {chat} with id {id}", currentChat.GroupName, groupId);
            var santasWithMatch = new List<Model.Participant>();

            //foreach (var participant in currentChat.Participants)
            //{
            //    participant.UnformattedText = participant.UnformattedText? .Replace("/start", "");
            //    participant.UnformattedText = participant.UnformattedText.Replace(SINGN_OUT, "");
            //    participant.UnformattedText = participant.UnformattedText.Replace(SHOW_INFO, "");
            //}
        

            var notMatchedSantas = currentChat.Participants.Where(p => !String.IsNullOrEmpty(p.UnformattedText) && p.UnformattedText.Length > 7 && p.ParticipantStatus != Model.ParticipantStatus.cancelled).ToList();


            Random r = new Random();
            var restTargetCount = notMatchedSantas.Count;
            var firstSanta = notMatchedSantas[r.Next(restTargetCount)];
            firstSanta.SantaMatching = new Model.SantaMatching();
            var santa = firstSanta;
            restTargetCount--;
            notMatchedSantas.Remove(santa);


            _logger.LogInformation("Found {potentialSantas} santas", notMatchedSantas.Count);


            while (restTargetCount > 0)
            {

                //default behaivior is to find someone to send the present to

                var nextRandomIndexOfGlobalSantas = r.Next(notMatchedSantas.Count);
                var target = notMatchedSantas[nextRandomIndexOfGlobalSantas];


                //link the giver and receiver
                if (santa.SantaMatching == null)
                {
                    santa.SantaMatching = new Model.SantaMatching();
                }
                santa.SantaMatching.SendingToId = target.Id;
                if (target.SantaMatching == null)
                {
                    target.SantaMatching = new Model.SantaMatching();
                }
                target.SantaMatching.ReceivingFromId = santa.Id;

                santasWithMatch.Add(santa);
                santa = target;
                restTargetCount--;
                notMatchedSantas.Remove(santa);

            }

            santa.SantaMatching.SendingToId = firstSanta.Id;
            firstSanta.SantaMatching.ReceivingFromId = santa.Id;
            santasWithMatch.Add(santa);
            List<(string Santa, string Grandson)> ret = santasWithMatch.Select(p =>
                    (Santa: santasWithMatch.FirstOrDefault(x => x.Id == p.SantaMatching?.ReceivingFromId)?.AccountName,
                    Grandson: santasWithMatch.FirstOrDefault(x => x.Id == p.SantaMatching?.SendingToId)?.AccountName))
                .ToList();



            _logger.LogDebug("Saving chat group to DB");

            _repository.Save(currentChat);
            _logger.LogDebug("Creating tuple of matched names");


            return ret;

        }



    }
}
