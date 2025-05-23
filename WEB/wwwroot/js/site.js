console.log('Script loaded!');

// Функции для улучшения работы на мобильных устройствах
document.addEventListener('DOMContentLoaded', function() {
    // Mobile menu handling
    const hamburger = document.querySelector('.navbar-toggler');
    const navbarCollapse = document.querySelector('.navbar-collapse');
    
    console.log('Hamburger element:', hamburger);
    console.log('Navbar collapse element:', navbarCollapse);
    
    // Удаляем класс menu-open при загрузке страницы, чтобы начать с правильного состояния
    document.body.classList.remove('menu-open');
    
    // Функция для обработки клика по кнопке меню
    function toggleMenu(event) {
        event.preventDefault();
        event.stopPropagation();
        console.log('Hamburger clicked');
        
        // Проверяем, открыто ли меню
        const isOpen = navbarCollapse.classList.contains('show');
        console.log('Menu is currently ' + (isOpen ? 'open' : 'closed'));
        
        // Если меню закрыто, открываем его
        if (!isOpen) {
            navbarCollapse.classList.add('show');
            document.body.classList.add('menu-open');
            console.log('Menu opened via direct handler');
        } else {
            // Если меню открыто, закрываем его
            navbarCollapse.classList.remove('show');
            document.body.classList.remove('menu-open');
            console.log('Menu closed via direct handler');
        }
    }
    
    // Добавляем прямой обработчик для кнопки меню
    if (hamburger && navbarCollapse) {
        // Удаляем предыдущие обработчики, если они есть
        hamburger.removeEventListener('click', toggleMenu);
        
        // Добавляем обработчик клика
        hamburger.addEventListener('click', toggleMenu, { passive: false });
    }
    
    // Отслеживаем события Bootstrap для меню
    if (navbarCollapse) {
        navbarCollapse.addEventListener('show.bs.collapse', function() {
            console.log('Menu is opening');
            document.body.classList.add('menu-open');
        });
        
        navbarCollapse.addEventListener('hide.bs.collapse', function() {
            console.log('Menu is closing');
            document.body.classList.remove('menu-open');
        });
        
        // Закрытие меню при клике на ссылку
        const navLinks = document.querySelectorAll('.navbar-nav .nav-link');
        navLinks.forEach(link => {
            link.addEventListener('click', () => {
                if (navbarCollapse && navbarCollapse.classList.contains('show')) {
                    navbarCollapse.classList.remove('show');
                    document.body.classList.remove('menu-open');
                    console.log('Menu closed via link click');
                }
            });
        });
    }
    
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
        if (event.scale !== 1 && event.touches.length > 1) {
            event.preventDefault();
        }
    }, { passive: true });

    // Улучшаем отклик при нажатии
    document.addEventListener('touchstart', function(){}, {passive: true});

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

    // Закрытие меню при клике на затемненную область
    document.addEventListener('click', function(e) {
        if (document.body.classList.contains('menu-open') && 
            navbarCollapse && 
            !navbarCollapse.contains(e.target) && 
            hamburger && 
            e.target !== hamburger && 
            !hamburger.contains(e.target)) {
            
                navbarCollapse.classList.remove('show');
                document.body.classList.remove('menu-open');
            console.log('Menu closed via overlay click');
        }
                });
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