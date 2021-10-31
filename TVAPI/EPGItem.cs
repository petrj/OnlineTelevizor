using System;

namespace TVAPI
{
    public class EPGItem : JSONObject
    {
        public string ChannelId { get; set; }

        public string EPGId { get; set; }

        public string Title { get; set; }
        public string Description { get; set; }

        public DateTime Start { get; set; }
        public DateTime Finish { get; set; }

        public double Progress
        {
            get
            {
                if (Finish == DateTime.MinValue || Start == DateTime.MinValue ||
                    Finish == DateTime.MaxValue || Finish == DateTime.MaxValue ||
                    Start > DateTime.Now || Start>Finish)
                    return 0;

                if (Finish < DateTime.Now)
                    return 1;

                var totalSecs = (Finish - Start).TotalSeconds;
                var futureSecs = (Finish - DateTime.Now).TotalSeconds;

                return 1 - futureSecs/totalSecs;
            }
        }

        public string Time
        {
            get
            {
                if (Finish == DateTime.MinValue || Start == DateTime.MinValue ||
                    Finish == DateTime.MaxValue || Finish == DateTime.MaxValue)
                    return String.Empty;

                return $"{Start.ToString("HH:mm")}-{Finish.ToString("HH:mm")}";
            }
        }
    }
}
