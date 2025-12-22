export default class RobotObjectUtility {}

RobotObjectUtility.getPluginIcon = function (objectId) {
  switch (objectId) {
    case "CpuEvent":
      return "developer_board";

    case "DateTimeEvent":
      return "calendar_month";

    case "DiskSpaceEvent":
      return "save";

    case "ExcelFileTask":
      return "table_view";

    case "FileSystemEvent":
      return "folder";

    case "FileSystemTask":
      return "folder_special";

    case "FtpSftpTask":
      return "cloud_upload";

    case "MemoryEvent":
      return "memory";

    case "OSRobotServiceStartEvent":
      return "settings";

    case "ReadTextFileTask":
      return "description";

    case "ReadBinaryFileTask":
      return "save";

    case "RESTApiTask":
      return "api";

    case "RunProgramTask":
      return "terminal";

    case "SendEMailTask":
      return "mail";

    case "SqlServerBackupTask":
      return "settings_backup_restore";

    case "SqlServerBulkCopyTask":
      return "content_copy";

    case "SqlServerCommandTask":
      return "touch_app";

    case "SystemEventsEvent":
      return "event_repeat";

    case "UnzipTask":
      return "folder_zip";

    case "WriteBinaryFileTask":
      return "notes";

    case "WriteTextFileTask":
      return "edit_note";

    case "ZipTask":
      return "folder_zip";

    case "PingTask":
      return "network_ping";
  }
};
