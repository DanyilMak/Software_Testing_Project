import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
    stages: [
        { duration: '30s', target: 50 }, // Поступове збільшення до 50 користувачів
        { duration: '1m', target: 50 },  // Стабільне навантаження 1 хвилина
        { duration: '30s', target: 0 },  // Поступове зменшення до 0
    ],
    thresholds: {
        http_req_duration: ['p(95)<500'], // 95% запитів < 500ms
    },
};

export default function () {
    // Навантажувальне тестування важкого запиту таблиці лідерів
    const res = http.get('http://localhost:5284/api/quizzes/1/leaderboard');
    
    check(res, {
        'status is 200': (r) => r.status === 200,
        'returns array': (r) => r.body.includes('['), // Перевіряємо, що повернувся масив
    });
    
    sleep(1);
}