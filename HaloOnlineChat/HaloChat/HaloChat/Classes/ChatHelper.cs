using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HaloChat.Classes
{
    public class ChatHelper : INotifyPropertyChanged
    {
        string chatInput = string.Empty;
        public double ChatSize { get; set; }
        public double InputSize { get; set; }
        FixedSizeObservable<string> chatMessages = new FixedSizeObservable<string>(20) { "Guacamole Started.\nDip Away!\n" };

        
        public ChatHelper()
        {
            ChatSize = 16;
            InputSize = 24;
        }
        
        public string ChatInput
        {
            get
            {
                return chatInput;
            }
            set
            {
                chatInput = value;
                OnPropertyChanged("ChatInput");
            }
        }

        public void ProcessText()
        {
            ChatInput = String.Empty;
        }

        public FixedSizeObservable<string> ChatMessages
        {
            get
            {
                return chatMessages;
            }
            set
            {
                chatMessages = value;
                OnPropertyChanged("ChatMessages");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        void OnPropertyChanged(string propertyName)
        {
            if (null != PropertyChanged)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
