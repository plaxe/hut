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

    // Улучшение мобильного меню
    if (window.innerWidth < 768) {
        const navbarCollapse = document.querySelector('.navbar-collapse');
        const navbarToggler = document.querySelector('.navbar-toggler');
        
        if (navbarCollapse && navbarToggler) {
            // Создаем и добавляем кнопку закрытия в меню
            const closeButton = document.createElement('button');
            closeButton.className = 'close-menu-btn';
            closeButton.innerHTML = '&times;';
            closeButton.setAttribute('aria-label', 'Close menu');
            navbarCollapse.appendChild(closeButton);
            
            // Функция для открытия меню
            function openMenu() {
                navbarCollapse.classList.add('show');
                document.body.classList.add('menu-open');
                
                // Предотвращаем прокрутку основного содержимого
                document.body.style.overflow = 'hidden';
            }
            
            // Функция для закрытия меню
            function closeMenu() {
                navbarCollapse.classList.remove('show');
                document.body.classList.remove('menu-open');
                
                // Возвращаем прокрутку
                document.body.style.overflow = '';
            }
            
            // Обработчик для открытия меню
            navbarToggler.addEventListener('click', function(e) {
                e.preventDefault();
                openMenu();
            });
            
            // Обработчик для кнопки закрытия
            closeButton.addEventListener('click', function() {
                closeMenu();
            });
            
            // Копируем логотип из шапки в мобильное меню, если его там еще нет
            const mainLogo = document.querySelector('.logo');
            const menuLogo = navbarCollapse.querySelector('.logo');
            
            if (mainLogo && !menuLogo) {
                const logoClone = mainLogo.cloneNode(true);
                navbarCollapse.insertBefore(logoClone, navbarCollapse.firstChild);
            }
            
            // Добавляем контакты внизу мобильного меню
            if (!navbarCollapse.querySelector('.mobile-contacts')) {
                const contactsDiv = document.createElement('div');
                contactsDiv.className = 'mobile-contacts';
                
                // E-mail контакт
                const emailLink = document.createElement('a');
                emailLink.href = 'mailto:uakhutir@gmail.com';
                emailLink.textContent = 'uakhutir@gmail.com';
                
                // Телефонный контакт
                const phoneLink = document.createElement('a');
                phoneLink.href = 'tel:+380974018115';
                phoneLink.textContent = '+38 097 401 81 15';
                
                contactsDiv.appendChild(emailLink);
                contactsDiv.appendChild(phoneLink);
                
                navbarCollapse.appendChild(contactsDiv);
            }
            
            // Закрываем меню при клике по пунктам меню
            const navLinks = document.querySelectorAll('.navbar-collapse .nav-link');
            navLinks.forEach(function(link) {
                link.addEventListener('click', function() {
                    closeMenu();
                });
            });
            
            // Закрываем меню при свайпе влево
            let touchStartX = 0;
            let touchEndX = 0;
            
            navbarCollapse.addEventListener('touchstart', function(e) {
                touchStartX = e.changedTouches[0].screenX;
            }, false);
            
            navbarCollapse.addEventListener('touchend', function(e) {
                touchEndX = e.changedTouches[0].screenX;
                handleSwipe();
            }, false);
            
            function handleSwipe() {
                // Если свайп влево (значение touchStartX больше touchEndX)
                if (touchStartX - touchEndX > 50) {
                    closeMenu();
                }
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

// Функция для обновления мобильного меню
function updateMobileMenu() {
    const navbarCollapse = document.querySelector('.navbar-collapse');
    const navbarToggler = document.querySelector('.navbar-toggler');
    
    if (!navbarCollapse || !navbarToggler) return;
    
    // Если это мобильное устройство
    if (window.innerWidth < 768) {
        // Устанавливаем правильные стили для мобильного меню
        navbarCollapse.style.position = 'fixed';
        navbarCollapse.style.top = '0';
        navbarCollapse.style.width = '100vw';
        navbarCollapse.style.height = '100vh';
        navbarCollapse.style.backgroundColor = '#000';
        navbarCollapse.style.zIndex = '9990';
        navbarCollapse.style.padding = '80px 30px 30px 30px';
        navbarCollapse.style.display = 'flex';
        navbarCollapse.style.flexDirection = 'column';
        
        // Если кнопка закрытия еще не добавлена
        if (!document.querySelector('.close-menu-btn')) {
            const closeButton = document.createElement('button');
            closeButton.className = 'close-menu-btn';
            closeButton.innerHTML = '&times;';
            closeButton.setAttribute('aria-label', 'Close menu');
            navbarCollapse.appendChild(closeButton);
            
            // Добавляем обработчик для кнопки закрытия
            closeButton.addEventListener('click', function() {
                navbarCollapse.classList.remove('show');
                document.body.classList.remove('menu-open');
                document.body.style.overflow = '';
            });
        }
        
        // Очищаем содержимое, кроме кнопки закрытия
        const closeButton = document.querySelector('.close-menu-btn');
        if (closeButton) {
            const parent = closeButton.parentNode;
            while (parent.firstChild) {
                if (parent.firstChild !== closeButton) {
                    parent.removeChild(parent.firstChild);
                } else {
                    break;
                }
            }
        }
        
        // 1. Добавляем мелкий текст сверху
        const introText = document.createElement('div');
        introText.className = 'mobile-intro-text';
        introText.innerHTML = 'ЮА.Хутір – це не просто продукти, це шлях до здорового життя. Довіряйте якості, обирайте ЮА.Хутір – гармонія природи та сучасності в кожному продукті.';
        introText.style.color = 'white';
        introText.style.fontSize = '14px';
        introText.style.lineHeight = '1.4';
        introText.style.marginBottom = '30px';
        introText.style.paddingRight = '30px';
        navbarCollapse.appendChild(introText);
        
        // 2. Добавляем логотип
        const mainLogo = document.querySelector('.logo');
        if (mainLogo) {
            const logoClone = mainLogo.cloneNode(true);
            logoClone.style.marginBottom = '40px';
            navbarCollapse.appendChild(logoClone);
        }
        
        // 3. Добавляем меню
        const navItems = document.querySelector('.navbar-nav');
        if (navItems) {
            const menuClone = navItems.cloneNode(true);
            menuClone.style.marginBottom = '40px';
            
            // Стилизуем пункты меню
            const navLinks = menuClone.querySelectorAll('.nav-link');
            navLinks.forEach(link => {
                link.style.color = 'white';
                link.style.fontSize = '24px';
                link.style.fontWeight = 'normal';
                link.style.marginBottom = '20px';
                link.style.display = 'block';
                link.style.textAlign = 'left';
                link.style.padding = '0';
                
                // Добавляем обработчик для закрытия меню при клике
                link.addEventListener('click', function() {
                    navbarCollapse.classList.remove('show');
                    document.body.classList.remove('menu-open');
                    document.body.style.overflow = '';
                });
            });
            
            navbarCollapse.appendChild(menuClone);
        }
        
        // 4. Добавляем переключатель языка
        const langSwitcher = document.createElement('div');
        langSwitcher.className = 'language-switch';
        langSwitcher.innerHTML = 'Мова: <span>En</span>';
        langSwitcher.style.color = 'white';
        langSwitcher.style.marginTop = 'auto';
        langSwitcher.style.marginBottom = '20px';
        langSwitcher.style.borderTop = '1px solid rgba(255, 255, 255, 0.2)';
        langSwitcher.style.paddingTop = '20px';
        navbarCollapse.appendChild(langSwitcher);
        
        // 5. Добавляем контакты
        const contactsDiv = document.createElement('div');
        contactsDiv.className = 'mobile-contacts';
        
        // Телефонный контакт
        const phoneLink = document.createElement('a');
        phoneLink.href = 'tel:+380974018115';
        phoneLink.textContent = '+38 097 401 88 15';
        phoneLink.style.color = 'white';
        phoneLink.style.textDecoration = 'none';
        phoneLink.style.display = 'block';
        phoneLink.style.padding = '15px 0';
        phoneLink.style.textAlign = 'center';
        phoneLink.style.border = '1px solid white';
        phoneLink.style.borderRadius = '30px';
        phoneLink.style.marginBottom = '10px';
        
        // E-mail контакт
        const emailLink = document.createElement('a');
        emailLink.href = 'mailto:uakhutir@gmail.com';
        emailLink.textContent = 'uakhutir@gmail.com';
        emailLink.style.color = 'white';
        emailLink.style.textDecoration = 'none';
        emailLink.style.display = 'block';
        emailLink.style.padding = '15px 0';
        emailLink.style.textAlign = 'center';
        emailLink.style.border = '1px solid white';
        emailLink.style.borderRadius = '30px';
        
        contactsDiv.appendChild(phoneLink);
        contactsDiv.appendChild(emailLink);
        contactsDiv.style.marginBottom = '20px';
        
        navbarCollapse.appendChild(contactsDiv);
        
        // Обновляем стили кнопки открытия меню
        navbarToggler.style.zIndex = '9999';
        
        // Добавляем обработчики для свайпа
        let touchStartX = 0;
        let touchEndX = 0;
        
        navbarCollapse.addEventListener('touchstart', function(e) {
            touchStartX = e.changedTouches[0].screenX;
        }, { passive: true });
        
        navbarCollapse.addEventListener('touchend', function(e) {
            touchEndX = e.changedTouches[0].screenX;
            if (touchStartX - touchEndX > 50) {
                navbarCollapse.classList.remove('show');
                document.body.classList.remove('menu-open');
                document.body.style.overflow = '';
            }
        }, { passive: true });
        
    } else {
        // На десктопе возвращаем стандартные стили
        navbarCollapse.style.position = '';
        navbarCollapse.style.top = '';
        navbarCollapse.style.width = '';
        navbarCollapse.style.height = '';
        navbarCollapse.style.backgroundColor = '';
        navbarCollapse.style.zIndex = '';
        navbarCollapse.style.padding = '';
        navbarCollapse.style.display = '';
        navbarCollapse.style.flexDirection = '';
        
        // Удаляем кнопку закрытия, если она есть
        const closeButton = document.querySelector('.close-menu-btn');
        if (closeButton) {
            closeButton.remove();
        }
        
        // Восстанавливаем оригинальное меню, если оно было изменено
        const originalNav = document.querySelector('.navbar-nav:not(.cloned)');
        if (originalNav) {
            navbarCollapse.appendChild(originalNav);
        }
        
        // Удаляем все добавленные элементы
        const introText = navbarCollapse.querySelector('.mobile-intro-text');
        if (introText) introText.remove();
        
        const clonedLogo = navbarCollapse.querySelector('.logo');
        if (clonedLogo) clonedLogo.remove();
        
        const langSwitcher = navbarCollapse.querySelector('.language-switch');
        if (langSwitcher) langSwitcher.remove();
        
        const mobileContacts = navbarCollapse.querySelector('.mobile-contacts');
        if (mobileContacts) mobileContacts.remove();
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
        updateMobileMenu();
    } else {
        // Возвращаем стандартные стили для десктопа
        updateFooter();
        updateMobileMenu();
    }
}

// Проверяем при загрузке страницы и при изменении размера окна
window.addEventListener('DOMContentLoaded', function() {
    checkMobile();
    // Запускаем один раз для мобильных устройств
    if (window.innerWidth < 768) {
        updateDividers();
        updateFooter();
        updateMobileMenu();
    }
});
window.addEventListener('resize', checkMobile); 