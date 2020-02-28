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
    private readonly StringBuilder buffer = new StringBuilder();
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
            throw new FileNotFoundException($"Could not find file {filePath}");
        }

        file = new FileInfo(filePath);

        timer.Elapsed += Timer_Elapsed;
    }

    public TailNET(string filePath, int interval) : this(filePath, Environment.NewLine, interval)
    {
    }

    public TailNET(string filePath, Encoding encoding) : this(filePath, Environment.NewLine, 500, encoding)
    {
    }

    public TailNET(string filePath, string delimiter) : this(filePath)
    {
        // Delimiter can not be null or empty. It would throw an exception  while processing.
        if (delimiter is null || delimiter is "")
        {
            throw new ArgumentException("No null or empty string allowed");
        }

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

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Stil", "IDE0063:Einfache using-Anweisung verwenden", Justification = "<Ausstehend>")]
    private void Timer_Elapsed(object sender, ElapsedEventArgs e)
    {
        if (!File.Exists(file.FullName))
        {
            Stop();
            FileDeleted?.Invoke(this, EventArgs.Empty);
            Debug.WriteLine("File deleted");
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
                    oldSize = file.Length;
                    Debug.WriteLine("Initial file size set to " + oldSize);
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
                    Debug.WriteLine($"File size has decreased. Reset initial file size to {newSize}");

                    oldSize = newSize;

                    Debug.WriteLine("Clear buffer");

                    buffer.Clear();

                    return;
                }

                Debug.WriteLine($"Old size {oldSize} | New size {newSize}");

                using (FileStream fileStream = File.Open(file.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    using (StreamReader sr = new StreamReader(fileStream, encoding))
                    {
                        sr.BaseStream.Seek(oldSize, SeekOrigin.Begin);
                        
                        while (!sr.EndOfStream)
                        {
                            buffer.Append((Char)sr.Read());

                            // If the delimiter is bigger, we don't have to do anything.
                            if (buffer.Length >= delimiter.Length)
                            {
                                // We check if the only last character of the delimiter and buffer are the same.
                                // If they are not equal, no further processing is needed.
                                // This way the performance can be improved drastically.
                                // If delimiter is null or empty, it will throw an exception.
                                if (delimiter[^1] == buffer[^1])
                                {
                                    if (buffer.ToString().EndsWith(delimiter, StringComparison.Ordinal))
                                    {
                                        buffer.Remove(buffer.Length - delimiter.Length, delimiter.Length);
                                        LineAdded?.Invoke(this, buffer.ToString());
                                        buffer.Clear();
                                    }
                                }
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

        if (ResetBeforeRestart && oldSize != -1)
        {
            // The state of the file object has to be refreshed, to get the current file size
            file.Refresh();
            oldSize = file.Length;
            Debug.WriteLine("Reset file size to " + oldSize);
        }

        timer.Start();
        Started?.Invoke(this, EventArgs.Empty);
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
        Stopped?.Invoke(this, EventArgs.Empty);
        Debug.WriteLine("Monitoring stopped");
    }

    #endregion Methods
}