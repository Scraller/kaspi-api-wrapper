import React from 'react';

interface AlertProps {
    message: string;
    type?: 'success' | 'error' | 'info' | 'warning';
    onClose?: () => void;
}

const Alert: React.FC<AlertProps> = ({ message, type = 'info', onClose }) => {
    const alertClass = `alert alert-${type}`;

    return (
        <div className={alertClass}>
            <span>{message}</span>
            {onClose && (
                <button onClick={onClose} className="alert-close">
                    &times;
                </button>
            )}
        </div>
    );
};

export default Alert;