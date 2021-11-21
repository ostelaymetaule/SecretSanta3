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
        public List<(string Santa, string Grandson)> TriggerMatching(long chatId)
        {

            var currentChat = _repository.GetById(chatId);
            _logger.LogDebug("Prepare to match santas in chat {chat} with id {id}", currentChat.GroupName, chatId);

            var allParticipantsWithAddress = currentChat.Participants.Where(p => !String.IsNullOrEmpty(p.PostalAddress)).ToList();
            int potentialSantas = allParticipantsWithAddress.Count();
            _logger.LogInformation("Found {potentialSantas} santas", potentialSantas);

            var currentSanta = allParticipantsWithAddress[Random.Shared.Next(0, potentialSantas)];
            var firstSanta = currentSanta;
            allParticipantsWithAddress.Remove(currentSanta);
            potentialSantas--;
            Model.Participant target;
            while (allParticipantsWithAddress.Any())
            {
                //default behaivior is to find someone to send the present to
                int nextMatchingIndexOfParticipants = Random.Shared.Next(0, potentialSantas);
                target = allParticipantsWithAddress[nextMatchingIndexOfParticipants];

                if (currentSanta.CanSendTo == Model.LocationMarker.Country)
                {
                    //But if we can we should consider the preferences of the same-country-post
                    var sameCountryParticipants = allParticipantsWithAddress.Where(p => p.Location == currentSanta.Location).ToList();
                    if (sameCountryParticipants.Any())
                    {
                        int nextMatchingIndexOfLocalParticipants = Random.Shared.Next(0, sameCountryParticipants.Count);
                        target = sameCountryParticipants[nextMatchingIndexOfLocalParticipants];
                    }
                    else
                    {
                        _logger.LogDebug("no matching same country participants, use global list");

                    }
                }
                //link the giver and receiver
                currentSanta.SantaMatching.SendingToId = target.Id;
                target.SantaMatching.ReceivingFromId = currentSanta.Id;
                currentSanta = target;
                allParticipantsWithAddress.Remove(currentSanta);
                potentialSantas--;
            }
            firstSanta.SantaMatching.SendingToId = currentSanta.Id;
            currentSanta.SantaMatching.ReceivingFromId = firstSanta.Id;
            _logger.LogDebug("Saving chat group to DB");

            _repository.Save(currentChat);
            _logger.LogDebug("Creating tuple of matched names");

            List<(string Santa, string Grandson)> ret = allParticipantsWithAddress.Select(p =>
                    (Santa: allParticipantsWithAddress.FirstOrDefault(x => x.Id == p.SantaMatching.ReceivingFromId).FullName,
                    Grandson: allParticipantsWithAddress.FirstOrDefault(x => x.Id == p.SantaMatching.SendingToId).FullName))
                .ToList();
            return ret;

        }



    }
}
