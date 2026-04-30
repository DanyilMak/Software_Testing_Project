import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
    vus: 2, // 2 віртуальні користувачі
    duration: '30s', // Тривалість 30 секунд
    thresholds: {
        http_req_duration: ['p(95)<500'], // 95% запитів < 500ms
        http_req_failed: ['rate<0.01'],   // менше 1% помилок
    },
};

export default function () {
    // Звичайний запит на отримання опублікованих вікторин
    const res = http.get('http://localhost:5284/api/quizzes');
    
    check(res, {
        'status is 200': (r) => r.status === 200,
        'has json content': (r) => r.headers['Content-Type'].includes('application/json'),
    });
    
    sleep(1);
}