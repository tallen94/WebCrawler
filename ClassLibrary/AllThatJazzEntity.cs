using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassLibrary {
    public class AllThatJazzEntity : TableEntity {
        public AllThatJazzEntity(string pk, string rk) {
            this.PartitionKey = pk;
            this.RowKey = rk;
        }
        public AllThatJazzEntity() { }

        public string honky { get; set; }
        public string tonky { get; set; }
    }
}
