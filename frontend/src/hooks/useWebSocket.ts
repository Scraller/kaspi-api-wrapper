import { useEffect, useRef } from 'react';

const useWebSocket = (url: string, onMessage: (data: any) => void) => {
    const socketRef = useRef<WebSocket | null>(null);

    useEffect(() => {
        socketRef.current = new WebSocket(url);

        socketRef.current.onmessage = (event) => {
            const data = JSON.parse(event.data);
            onMessage(data);
        };

        socketRef.current.onopen = () => {
            console.log('WebSocket connection established');
        };

        socketRef.current.onclose = () => {
            console.log('WebSocket connection closed');
        };

        return () => {
            if (socketRef.current) {
                socketRef.current.close();
            }
        };
    }, [url, onMessage]);

    return socketRef.current;
};

export default useWebSocket;