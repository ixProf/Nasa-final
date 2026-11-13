import React from 'react';
import ReactDOM from 'react-dom/client';
import { App } from './App';
import { LanguageProvider } from './context/LanguageContext';
import { TelemetryProvider } from './context/TelemetryContext';
import './styles/index.css';

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <LanguageProvider>
      <TelemetryProvider>
        <App />
      </TelemetryProvider>
    </LanguageProvider>
  </React.StrictMode>
);

