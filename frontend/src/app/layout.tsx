import React from 'react';
import './globals.css';

const Layout = ({ children }) => {
    return (
        <div className="layout">
            <header>
                <h1>Kaspi Price Tracker</h1>
            </header>
            <main>{children}</main>
            <footer>
                <p>&copy; {new Date().getFullYear()} Kaspi Price Tracker. All rights reserved.</p>
            </footer>
        </div>
    );
};

export default Layout;