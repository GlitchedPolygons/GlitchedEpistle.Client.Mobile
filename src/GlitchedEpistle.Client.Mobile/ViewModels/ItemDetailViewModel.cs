using System;

using GlitchedEpistle.Client.Mobile.Models;

namespace GlitchedEpistle.Client.Mobile.ViewModels
{
    public class ItemDetailViewModel : BaseViewModel
    {
        public Item Item { get; set; }
        public ItemDetailViewModel(Item item = null)
        {
            Title = item?.Text;
            Item = item;
        }
    }
}
