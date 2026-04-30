import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
    stages: [
        { duration: '30s', target: 50 },  // Розгін до 50
        { duration: '30s', target: 150 }, // Різкий стрибок до 150
        { duration: '30s', target: 300 }, // Стрибок до 300 (Точка потенційної відмови)
        { duration: '30s', target: 0 },   // Швидке відновлення
    ],
    thresholds: {
        http_req_duration: ['p(95)<1000'], // У стрес-тестах припускається більша затримка
    },
};

export default function () {
    // Симуляція надсилання відповідей (обираємо відповіді з Id 1 та 2)
    const payload = JSON.stringify([1, 2]);
    const params = {
        headers: { 'Content-Type': 'application/json' },
    };

    // Надсилаємо відповіді для спроби з Id 1
    const res = http.post('http://localhost:5284/api/attempts/1/submit', payload, params);
    
    check(res, {
        // У стрес-тесті ми допускаємо помилки 400 (якщо спроба вже завершена), 
        // головне, щоб сервер не впав з 500 помилкою
        'server not crashed (status != 500)': (r) => r.status !== 500,
    });
    
    sleep(1);
}