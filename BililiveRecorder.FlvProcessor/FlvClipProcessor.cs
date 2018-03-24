﻿using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BililiveRecorder.FlvProcessor
{
    public class FlvClipProcessor
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public readonly FlvMetadata Header;
        public readonly List<FlvTag> HTags;
        public readonly List<FlvTag> Tags;
        private int target = -1;

        public Func<string> GetFileName;

        public FlvClipProcessor(FlvMetadata header, List<FlvTag> head, List<FlvTag> past, int future)
        {
            Header = header;
            HTags = head;
            Tags = past;
            target = Tags[Tags.Count - 1].TimeStamp + (future * FlvStreamProcessor.SEC_TO_MS);
            logger.Trace("Clip 创建 Tags.Count={0} Tags[0].TimeStamp={1} Tags[Tags.Count-1].TimeStamp={2} Tags里秒数={3}",
                Tags.Count, Tags[0].TimeStamp, Tags[Tags.Count - 1].TimeStamp, (Tags[Tags.Count - 1].TimeStamp - Tags[0].TimeStamp) / 1000d);
        }

        public void AddTag(FlvTag tag)
        {
            Tags.Add(tag);
            if (tag.TimeStamp >= target)
            {
                FinallizeFile();
            }
        }

        public void FinallizeFile()
        {
            using (var fs = new FileStream(GetFileName(), FileMode.CreateNew, FileAccess.ReadWrite))
            {
                fs.Write(FlvStreamProcessor.FLV_HEADER_BYTES, 0, FlvStreamProcessor.FLV_HEADER_BYTES.Length);
                fs.Write(new byte[] { 0, 0, 0, 0, }, 0, 4);

                // TODO: debug 这里好像有问题。输出的文件时长不对
                Header.Meta["duration"] = Tags[Tags.Count - 1].TimeStamp / 1000.0;
                Header.Meta["lasttimestamp"] = (double)Tags[Tags.Count - 1].TimeStamp;

                var t = new FlvTag
                {
                    TagType = TagType.DATA,
                    Data = Header.ToBytes()
                };
                t.WriteTo(fs);

                HTags.ForEach(tag => tag.WriteTo(fs));
                Tags.ForEach(tag => tag.WriteTo(fs));

                fs.Close();
            }
            Tags.Clear();

            ClipFinalized?.Invoke(this, new ClipFinalizedArgs() { ClipProcessor = this });
        }

        public event ClipFinalizedEvent ClipFinalized;
    }
}
