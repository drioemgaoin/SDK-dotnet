﻿using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Quickblox.Sdk.GeneralDataModel.Filters;

namespace Quickblox.Sdk.GeneralDataModel.Request
{
    public abstract class BaseRequestSettings
    {
        /// <summary>
        /// Возвращает коллекцию хедеров и их значений.
        /// </summary>
        [JsonIgnore]
        [IgnoreDataMember]
        public IDictionary<String, String> Headers { get; set; }

        [JsonIgnore]
        [IgnoreDataMember]
        public FilterAggregator Filter { get; set; }
    }
}

