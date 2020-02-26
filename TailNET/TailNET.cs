using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Timers;

public class TailNET
{
    #region Fields

    private readonly FileInfo file;
    private readonly string delimiter = Environment.NewLine;
    private readonly System.Timers.Timer timer = new System.Timers.Timer() { Interval = 500 };
    private readonly Encoding encoding = Encoding.UTF8;
    private readonly object processingLock = new object();
    private string buffer;
    private long oldSize = -1;

    #endregion Fields

    #region Properties

    /// <summary>
    /// Determines whether the file size is reset before restarting. If true, all changes between stop and start are discarded.
    /// </summary>
    public bool ResetBeforeRestart { get; set; } = true;

    #endregion Properties

    #region Events

    /// <summary>
    /// Occurs when a new line is added to file.
    /// </summary>
    public event EventHandler<string> LineAdded;

    /// <summary>
    /// Occurs when the file is deleted
    /// </summary>
    public event EventHandler FileDeleted;

    /// <summary>
    /// Occurs when monitoring starts
    /// </summary>
    public event EventHandler Started;

    /// <summary>
    /// Occurs when monitoring stops
    /// </summary>
    public event EventHandler Stopped;

    #endregion Events

    #region Constructors

    public TailNET(string filePath)
    {
        if (!File.Exists(filePath))
        {
            FileDeleted?.Invoke(this, EventArgs.Empty);
            Stop();
            Debug.WriteLine("File deleted -> monitoring stopped");
        }

        file = new FileInfo(filePath);

        timer.Elapsed += Timer_Elapsed;
    }

    public TailNET(string filePath, int interval) : this(filePath, Environment.NewLine, interval)
    {
    }

    public TailNET(string filepath, Encoding encoding) : this(filepath, Environment.NewLine, 500, encoding)
    {
    }

    public TailNET(string filePath, string delimiter) : this(filePath)
    {
        this.delimiter = delimiter;
    }

    public TailNET(string filePath, string delimiter, int interval) : this(filePath, delimiter)
    {
        timer.Interval = interval;
    }

    public TailNET(string filePath, string delimiter, int interval, Encoding encoding) : this(filePath, delimiter, interval)
    {
        this.encoding = encoding;
    }

    #endregion Constructors

    #region Methods

    private void Timer_Elapsed(object sender, ElapsedEventArgs e)
    {
        if (!File.Exists(file.FullName))
        {
            return;
        }

        // Code will be skipped if still locked (processing running)
        if (Monitor.TryEnter(processingLock))
        {
            // When the lock is obtained, start processing
            try
            {
                // If still initial
                if (oldSize == -1)
                {
                    Debug.WriteLine("File still initial");
                    oldSize = file.Length;
                }

                // The current file size is needed
                long newSize = new FileInfo(file.FullName).Length;

                // If old size and current size are the same, we do not need further processing
                if (oldSize == newSize)
                {
                    return;
                }

                // If the current file size is smaller than the old size, the file has been emptied
                // The old size will be set to the current smaller size and the buffer will be emptied
                if (oldSize > newSize)
                {
                    Debug.WriteLine($"File size has decreased -> Reset initial file size to {newSize}");

                    oldSize = newSize;

                    Debug.WriteLine("Clear buffer");

                    buffer = String.Empty;

                    return;
                }

                Debug.WriteLine($"Old size {oldSize} | New size {newSize}");

                using (FileStream stream = File.Open(file.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    using (StreamReader sr = new StreamReader(stream, encoding))
                    {
                        sr.BaseStream.Seek(oldSize, SeekOrigin.Begin);

                        string data = sr.ReadToEnd();

                        // If it contains a delimiter, it has atleast one valid line
                        if (data.Contains(delimiter, StringComparison.Ordinal))
                        {
                            // Get position of last occurence of a delimiter + delimiter size
                            int lastIndexOfDelimiter = data.LastIndexOf(delimiter, StringComparison.Ordinal) + delimiter.Length;

                            // Everything until last index of delimiter + delimiter size is valid, which means it
                            // contains lines of text with a delimiter at the end. It will be saved in a temp variable
                            // together with the buffer data.
                            string validTempDATA = buffer + data.Substring(0, lastIndexOfDelimiter);

                            // We save the what's left over in the buffer
                            buffer = data.Substring(lastIndexOfDelimiter);

                            // Assign the valid value of the temp variable back to the data variable.
                            data = validTempDATA;
                        }
                        else
                        {
                            // If no delimiter found, the data does not contain a valid line.
                            // Therefore it is added to the buffer.
                            buffer += data;

                            // The data variable is set to empty to preven further processing
                            data = string.Empty;
                        }

                        if (!string.IsNullOrEmpty(data))
                        {
                            // The data will be splitted into substrings with the delimiter as separator
                            // Empty entries will be removed
                            string[] lines = data.Split(new[] { delimiter }, StringSplitOptions.RemoveEmptyEntries);

                            // Fire the event and send each line
                            foreach (string line in lines)
                            {
                                LineAdded?.Invoke(this, line.Trim());
                            }
                        }
                    }
                }

                oldSize = newSize;
            }
            finally
            {
                // Ensure that the lock is released.
                Monitor.Exit(processingLock);
            }
        }
    }

    /// <summary>
    /// Starts the file monitoring.
    /// </summary>
    public void Start()
    {
        if (timer.Enabled)
        {
            return;
        }

        if (ResetBeforeRestart)
        {
            // The state of the file object has to be refreshed, to get the current file size
            file.Refresh();
            oldSize = file.Length;
            Debug.WriteLine("Reset file size to " + oldSize);
        }

        timer.Start();
        Started(this, EventArgs.Empty);
        Debug.WriteLine("Monitoring started");
    }

    /// <summary>
    /// Stops the file monitoring.
    /// </summary>
    public void Stop()
    {
        if (!timer.Enabled)
        {
            return;
        }

        timer.Stop();
        Stopped(this, EventArgs.Empty);
        Debug.WriteLine("Monitoring stopped");
    }

    #endregion Methods
}