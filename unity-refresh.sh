#!/bin/bash
# Unity Editor Remote Control via Visual Studio Package UDP Protocol
# Sends messages to Unity Editor without requiring any custom Editor scripts

set -e

# Find Unity Editor PID
UNITY_PID=$(pgrep -f "Unity.app/Contents/MacOS/Unity" 2>/dev/null | head -1)

if [ -z "$UNITY_PID" ]; then
    echo "Error: Unity Editor not running"
    exit 1
fi

# Calculate messaging port: 56000 + (PID % 1000) + 2
PORT=$((56000 + (UNITY_PID % 1000) + 2))

# Message types (from MessageType.cs)
# None=0, Ping=1, Pong=2, Play=3, Stop=4, Pause=5, Unpause=6, Build=7, Refresh=8

send_message() {
    local msg_type=$1
    # Binary format: [Int32: MessageType] [Int32: 0 (empty string)]
    printf "\\x$(printf '%02x' $msg_type)\\x00\\x00\\x00\\x00\\x00\\x00\\x00" | nc -u -w1 127.0.0.1 $PORT
}

CMD="${1:-refresh}"

case "$CMD" in
    refresh)
        send_message 8
        echo "Sent Refresh to Unity (PID: $UNITY_PID, Port: $PORT)"
        ;;
    play)
        send_message 3
        echo "Sent Play to Unity (PID: $UNITY_PID, Port: $PORT)"
        ;;
    stop)
        send_message 4
        echo "Sent Stop to Unity (PID: $UNITY_PID, Port: $PORT)"
        ;;
    pause)
        send_message 5
        echo "Sent Pause to Unity (PID: $UNITY_PID, Port: $PORT)"
        ;;
    unpause)
        send_message 6
        echo "Sent Unpause to Unity (PID: $UNITY_PID, Port: $PORT)"
        ;;
    ping)
        send_message 1
        echo "Sent Ping to Unity (PID: $UNITY_PID, Port: $PORT)"
        ;;
    port)
        echo $PORT
        ;;
    *)
        echo "Usage: $0 [refresh|play|stop|pause|unpause|ping|port]"
        echo ""
        echo "Commands:"
        echo "  refresh  - Refresh AssetDatabase (default)"
        echo "  play     - Enter Play mode"
        echo "  stop     - Exit Play mode"
        echo "  pause    - Pause Play mode"
        echo "  unpause  - Unpause Play mode"
        echo "  ping     - Ping Unity Editor"
        echo "  port     - Print the messaging port"
        exit 1
        ;;
esac
