﻿using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Quickblox.Sdk.Modules.CustomObjectModule.Models
{
    public class SuccessfullyDeleted
    {
        [JsonProperty(PropertyName = "ids")]
        public List<Int32> Ids { get; set; }
    }
}
