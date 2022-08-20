using System;

namespace Streamer.AWS
{
    public interface Environment
    {

        String SessionTableName { get; set; }

        String StorageURL { get; set; }

        String Environment { get; set; }

    }
}
