import './styles/app.css';
import { createApp } from './app/App';

const rootEl = document.getElementById('app');
if (!rootEl) {
  throw new Error('Root element #app not found in the DOM.');
}

createApp(rootEl);
