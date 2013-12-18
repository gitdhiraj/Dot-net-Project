using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IntelliOpticsReport
{
    public class TripInfo
    {

        public TripInfo(string objectData) { ObjectData = objectData; }

        public TripInfo(bool isSelected, string objectData)
        {

            IsSelected = isSelected;

            ObjectData = objectData;

        }

        public Boolean IsSelected

        { get; set; }
        public String ObjectData

        { get; set; }

    }
}
