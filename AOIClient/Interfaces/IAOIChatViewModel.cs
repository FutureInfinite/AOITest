using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AOIClient.Interfaces
{
    internal interface IAOIChatViewModel
    {
        public DelegateCommand<object> AttemptRegisterCommand { get; set; }
        public DelegateCommand<object> AttemptConnectCommand { get; set; }
        public DelegateCommand<object> SendMessageCommand { get; set; }
    }
}
