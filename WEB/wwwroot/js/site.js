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

    // Закрытие мобильного меню при клике на пункт меню
    const navLinks = document.querySelectorAll('.navbar-nav .nav-link');
    const navbarToggler = document.querySelector('.navbar-toggler');
    const navbarCollapse = document.querySelector('.navbar-collapse');
    
    if (navLinks && navbarCollapse) {
        navLinks.forEach(function(link) {
            link.addEventListener('click', function() {
                if (navbarCollapse.classList.contains('show')) {
                    navbarToggler.click();
                }
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

    // Исправляем проблему с фиксированной позицией на iOS
    if (/iPad|iPhone|iPod/.test(navigator.userAgent)) {
        const fixedElements = document.querySelectorAll('.navbar');
        window.addEventListener('scroll', function() {
            fixedElements.forEach(function(element) {
                element.style.transform = 'translateZ(0)';
            });
        });
    }
    
    // Улучшаем отклик при нажатии
    document.addEventListener('touchstart', function(){}, {passive: true});

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

    // Улучшение отображения разделителей принципов на мобильных устройствах
    if (window.innerWidth < 768) {
        // Получаем контейнер с разделителями
        const dividerContainer = document.querySelector('.principles-dividers');
        
        // Если контейнер найден - работаем с ним
        if (dividerContainer) {
            // Устанавливаем стили для контейнера разделителей
            dividerContainer.style.width = '100%';
            dividerContainer.style.maxWidth = '100%';
            dividerContainer.style.boxSizing = 'border-box';
            
            // Удаляем внешние поля, которые могут влиять на выравнивание
            dividerContainer.style.marginLeft = '0';
            dividerContainer.style.marginRight = '0';
            
            // На очень маленьких экранах применяем другую технику
            if (window.innerWidth < 576) {
                // Проверяем родительский контейнер
                const parentContainer = dividerContainer.closest('.container');
                if (parentContainer) {
                    // Убираем отступы у родительского контейнера
                    parentContainer.style.paddingLeft = '0';
                    parentContainer.style.paddingRight = '0';
                    parentContainer.style.maxWidth = '100%';
                }
                
                // Делаем разделители на всю ширину экрана
                dividerContainer.style.width = '100vw';
                dividerContainer.style.position = 'relative';
                dividerContainer.style.left = '50%';
                dividerContainer.style.right = '50%';
                dividerContainer.style.marginLeft = '-50vw';
                dividerContainer.style.marginRight = '-50vw';
                dividerContainer.style.padding = '0 15px';
            }
        }
        
        // Получаем разделители и настраиваем их
        const dividers = document.querySelectorAll('.principles-dividers .divider');
        if (dividers.length > 0) {
            dividers.forEach((divider, index) => {
                // Базовые стили для всех разделителей
                divider.style.width = '100%';
                divider.style.margin = '0 auto';
                divider.style.display = 'block';
                divider.style.boxSizing = 'border-box';
                
                // Устанавливаем цвета в зависимости от индекса
                switch(index) {
                    case 0:
                        divider.style.backgroundColor = '#E9F0E6'; // Светло-зеленый
                        break;
                    case 1:
                        divider.style.backgroundColor = '#D4E1CF'; // Средне-зеленый
                        break;
                    case 2:
                        divider.style.backgroundColor = '#B5CEB1'; // Зеленый
                        break;
                }
            });
        }
    }

    // Улучшение отображения футера на мобильных устройствах
    if (window.innerWidth < 768) {
        // Улучшение отображения иконок социальных сетей
        const socialLinks = document.querySelectorAll('.social-link');
        if (socialLinks.length > 0) {
            socialLinks.forEach(link => {
                // Увеличиваем область касания
                link.style.width = '40px';
                link.style.height = '40px';
                link.style.display = 'flex';
                link.style.alignItems = 'center';
                link.style.justifyContent = 'center';
                
                // Увеличиваем размер иконок
                const img = link.querySelector('img');
                if (img) {
                    img.style.width = '24px';
                    img.style.height = '24px';
                }
            });
        }
        
        // Улучшение отображения логотипа в футере
        const footerLogo = document.querySelector('.footer-logo-text');
        if (footerLogo) {
            footerLogo.style.letterSpacing = '0.1em';
            footerLogo.style.marginTop = '20px';
            
            // Для очень маленьких экранов
            if (window.innerWidth < 576) {
                footerLogo.style.letterSpacing = '0.15em';
                footerLogo.style.wordSpacing = '0.2em';
            }
        }
    }
});

// Функция для обновления стилей разделителей
function updateDividers() {
    // Получаем и настраиваем контейнер с разделителями
    const dividerContainer = document.querySelector('.principles-dividers');
    if (!dividerContainer) return;
    
    // Базовые стили для всех размеров мобильных экранов
    dividerContainer.style.width = '100%';
    dividerContainer.style.maxWidth = '100%';
    dividerContainer.style.boxSizing = 'border-box';
    dividerContainer.style.margin = '15px 0';
    dividerContainer.style.overflow = 'hidden';
    
    // Получаем разделители и настраиваем их
    const dividers = document.querySelectorAll('.principles-dividers .divider');
    if (dividers.length === 0) return;
    
    // Проверяем, если это мобильное устройство
    if (window.innerWidth < 768) {
        // Отключаем эффект перекрытия на мобильных
        dividers.forEach((divider, index) => {
            // Сбрасываем все отступы и z-индексы для мобильных
            divider.style.margin = '0';
            divider.style.width = '100%';
            divider.style.display = 'block';
            divider.style.boxSizing = 'border-box';
            divider.style.position = 'relative';
            divider.style.zIndex = '1';
            
            // Устанавливаем правильные цвета
            if (index === 0) {
                divider.style.backgroundColor = '#E6EFE7';
            } else if (index === 1) {
                divider.style.backgroundColor = '#CDD9CD';
            } else if (index === 2) {
                divider.style.backgroundColor = '#B3C5B3';
            }
        });
    } else {
        // Возвращаем оригинальные стили для десктопа
        // (этот блок не нужен, если вы не меняете стили программно на десктопе)
    }
}

// Функция для обновления футера
function updateFooter() {
    // Получаем иконки соцсетей
    const socialLinks = document.querySelectorAll('.social-link');
    
    // Получаем логотип в футере
    const footerLogo = document.querySelector('.footer-logo-text');
    
    // Если это мобильное устройство (< 768px)
    if (window.innerWidth < 768) {
        // Улучшаем отображение иконок
        if (socialLinks.length > 0) {
            socialLinks.forEach(link => {
                link.style.width = '40px';
                link.style.height = '40px';
                link.style.display = 'flex';
                link.style.alignItems = 'center';
                link.style.justifyContent = 'center';
                
                const img = link.querySelector('img');
                if (img) {
                    img.style.width = '24px';
                    img.style.height = '24px';
                }
            });
        }
        
        // Улучшаем отображение логотипа
        if (footerLogo) {
            footerLogo.style.letterSpacing = '0.1em';
            footerLogo.style.marginTop = '20px';
            
            // Для очень маленьких экранов
            if (window.innerWidth < 576) {
                footerLogo.style.letterSpacing = '0.15em';
                footerLogo.style.wordSpacing = '0.2em';
                footerLogo.style.fontSize = '2.5rem';
                
                // Увеличиваем иконки еще больше
                socialLinks.forEach(link => {
                    link.style.width = '45px';
                    link.style.height = '45px';
                    
                    const img = link.querySelector('img');
                    if (img) {
                        img.style.width = '28px';
                        img.style.height = '28px';
                    }
                });
            } else {
                footerLogo.style.fontSize = '3rem';
            }
        }
    } else {
        // Возвращаем стандартные стили для десктопа
        if (socialLinks.length > 0) {
            socialLinks.forEach(link => {
                link.style.width = '';
                link.style.height = '';
                link.style.display = '';
                link.style.alignItems = '';
                link.style.justifyContent = '';
                
                const img = link.querySelector('img');
                if (img) {
                    img.style.width = '';
                    img.style.height = '';
                }
            });
        }
        
        if (footerLogo) {
            footerLogo.style.letterSpacing = '';
            footerLogo.style.marginTop = '';
            footerLogo.style.wordSpacing = '';
            footerLogo.style.fontSize = '';
        }
    }
}

// Добавляем класс для мобильных устройств
function checkMobile() {
    const viewportWidth = window.innerWidth || document.documentElement.clientWidth;
    document.body.classList.toggle('is-mobile', viewportWidth < 768);
    
    // Обновляем стили при изменении размера окна
    if (viewportWidth < 768) {
        updateDividers();
        updateFooter();
    } else {
        // Возвращаем стандартные стили для десктопа
        updateFooter();
    }
}

// Проверяем при загрузке страницы и при изменении размера окна
window.addEventListener('DOMContentLoaded', function() {
    checkMobile();
    // Запускаем один раз для мобильных устройств
    if (window.innerWidth < 768) {
        updateDividers();
        updateFooter();
    }
});
window.addEventListener('resize', checkMobile); 