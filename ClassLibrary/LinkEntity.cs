﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;

namespace ClassLibrary {
    public class LinkEntity : TableEntity {
        public LinkEntity(string pk, string rk) {
            this.PartitionKey = pk;
            this.RowKey = rk;
        }

        public LinkEntity() { }
        
        public string Link { get; set; }
        public string Title { get; set; }
        public string LinkTimestamp { get; set; }
    }
}
