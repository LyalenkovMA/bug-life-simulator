using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace TalesFromTheUnderbrush
{
    /// <summary>
    /// Интерфейс для объектов, которые можно сохранять и загружать
    /// </summary>
    public interface IPersistable
    {
        /// <summary>
        /// Уникальный идентификатор для сохранения
        /// </summary>
        string PersistentId { get; }

        /// <summary>
        /// Тип объекта для десериализации
        /// </summary>
        string PersistentType { get; }

        /// <summary>
        /// Нужно ли сохранять этот объект
        /// </summary>
        bool ShouldSave { get; }

        /// <summary>
        /// Сохранить состояние объекта
        /// </summary>
        PersistenceData Save();

        /// <summary>
        /// Загрузить состояние объекта
        /// </summary>
        void Load(PersistenceData data);

        /// <summary>
        /// Событие перед сохранением
        /// </summary>
        event Action<IPersistable> OnBeforeSave;

        /// <summary>
        /// Событие после загрузки
        /// </summary>
        event Action<IPersistable> OnAfterLoad;
    }    
}
