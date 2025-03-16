// Функции для улучшения работы на мобильных устройствах
document.addEventListener('DOMContentLoaded', function() {
    // Предотвращаем масштабирование при двойном нажатии
    let lastTouchEnd = 0;
    document.addEventListener('touchend', function(event) {
        const now = (new Date()).getTime();
        if (now - lastTouchEnd <= 300) {
            event.preventDefault();
        }
        lastTouchEnd = now;
    }, false);

    // Предотвращаем масштабирование при жесте сведения/разведения пальцев
    document.addEventListener('touchmove', function(event) {
        if (event.scale !== 1) {
            event.preventDefault();
        }
    }, { passive: false });

    // Улучшаем отклик при нажатии
    document.addEventListener('touchstart', function(){}, {passive: true});

    // Исправляем проблему с фиксированной позицией на iOS
    if (/iPad|iPhone|iPod/.test(navigator.userAgent)) {
        const fixedElements = document.querySelectorAll('.navbar');
        window.addEventListener('scroll', function() {
            fixedElements.forEach(function(element) {
                element.style.transform = 'translateZ(0)';
            });
        });
    }

    // Добавляем плавную прокрутку для якорных ссылок
    document.querySelectorAll('a[href^="#"]').forEach(anchor => {
        anchor.addEventListener('click', function (e) {
            e.preventDefault();
            
            const targetId = this.getAttribute('href');
            if (targetId === '#') return;
            
            const targetElement = document.querySelector(targetId);
            if (targetElement) {
                targetElement.scrollIntoView({
                    behavior: 'smooth',
                    block: 'start'
                });
            }
        });
    });

    // Улучшение работы с продуктовыми карточками на мобильных
    if (window.innerWidth < 768) {
        const productCards = document.querySelectorAll('.product-card-inner');
        if (productCards.length > 0) {
            productCards.forEach(card => {
                card.addEventListener('click', function() {
                    this.classList.toggle('flipped');
                });
                
                // Добавляем отдельный класс для лучшего управления
                card.classList.add('mobile-friendly');
            });
        }
    }
});

// Добавляем класс для мобильных устройств
function checkMobile() {
    const viewportWidth = window.innerWidth || document.documentElement.clientWidth;
    document.body.classList.toggle('is-mobile', viewportWidth < 768);
}

// Инициализация при загрузке страницы
window.addEventListener('DOMContentLoaded', function() {
    checkMobile();
});

// Обновление при изменении размера окна
window.addEventListener('resize', function() {
    checkMobile();
}); 