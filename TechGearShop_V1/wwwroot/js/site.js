// Global JS behavior for TechGear Shop Customer site

document.addEventListener("DOMContentLoaded", function () {
    // 1. Setup Toast notification style using SweetAlert2
    window.Toast = Swal.mixin({
        toast: true,
        position: 'bottom-end',
        showConfirmButton: false,
        timer: 3000,
        timerProgressBar: true,
        didOpen: (toast) => {
            toast.addEventListener('mouseenter', Swal.stopTimer)
            toast.addEventListener('mouseleave', Swal.resumeTimer)
        }
    });

    // Hiện Promo Popup Quảng cáo 1 lần duy nhất mỗi session
    if (!sessionStorage.getItem('promoShown')) {
        setTimeout(() => {
            const promoEl = document.getElementById('promoModal');
            if (promoEl) {
                const promoModal = new bootstrap.Modal(promoEl);
                promoModal.show();
                sessionStorage.setItem('promoShown', 'true');
            }
        }, 2000);
    }

    // 2. Intercept "Add to Cart" forms to process via AJAX
    document.querySelectorAll('.ajax-add-to-cart').forEach(form => {
        form.addEventListener('submit', async function (e) {
            e.preventDefault(); // Ngăn trình duyệt load lại trang

            const submitBtn = form.querySelector('button[type="submit"]');
            const originalHtml = submitBtn.innerHTML;

            // Effect loading
            submitBtn.innerHTML = '<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Đang xử lý...';
            submitBtn.disabled = true;

            const url = form.action;
            const formData = new FormData(form);

            try {
                const response = await fetch(url, {
                    method: 'POST',
                    body: formData,
                    headers: { 'X-Requested-With': 'XMLHttpRequest' }
                });

                const result = await response.json();

                if (result.success) {
                    Toast.fire({ icon: 'success', title: result.message });
                    // Update header badge live
                    updateCartBadge(result.cartCount);
                } else {
                    if (result.requireLogin) {
                        Swal.fire({
                            title: 'Chưa đăng nhập!',
                            text: 'Sếp vui lòng đăng nhập để thêm đồ vào giỏ hàng nhé.',
                            icon: 'warning',
                            showCancelButton: true,
                            confirmButtonText: 'Đăng nhập ngay',
                            cancelButtonText: 'Đóng'
                        }).then((dialog) => {
                            if (dialog.isConfirmed) {
                                window.location.href = '/Account/Login?ReturnUrl=' + encodeURIComponent(window.location.pathname + window.location.search);
                            }
                        });
                    } else {
                        Toast.fire({ icon: 'error', title: result.message });
                    }
                }
            } catch (err) {
                console.error(err);
                Toast.fire({ icon: 'error', title: 'Đã có lỗi xảy ra. Hãy thử lại.' });
            } finally {
                // Restore button
                submitBtn.innerHTML = originalHtml;
                submitBtn.disabled = false;
            }
        });
    });

    // Hàm update icon giỏ hàng trên Header bằng DOM thao tác
    function updateCartBadge(count) {
        const cartLink = document.querySelector('a[href="/Cart"]');
        if (cartLink) {
            let badge = cartLink.querySelector('.badge');
            if (!badge && count > 0) {
                // Chưa có badge thì insert html
                cartLink.insertAdjacentHTML('beforeend', `<span class="position-absolute top-10 start-100 translate-middle badge rounded-pill bg-danger">${count}<span class="visually-hidden">sản phẩm trong giỏ</span></span>`);
            } else if (badge && count > 0) {
                badge.innerHTML = `${count} <span class="visually-hidden">sản phẩm trong giỏ</span>`;
            } else if (badge && count <= 0) {
                badge.remove();
            }
        }
    }   // end updateCartBadge
});


/**
 * Custom Language Switcher — Dùng cookie `googtrans` chuẩn của Google.
 * Đây là cách đáng tin cậy 100%: set cookie rồi reload.
 */
function switchLang(langCode, flagEmoji, label) {
    const domain = location.hostname;
    const expire = new Date(Date.now() + 365 * 24 * 3600 * 1000).toUTCString();

    if (langCode === 'vi') {
        // Xóa cookie để về ngôn ngữ gốc
        document.cookie = 'googtrans=; expires=Thu, 01 Jan 1970 00:00:00 UTC; path=/;';
        document.cookie = `googtrans=; expires=Thu, 01 Jan 1970 00:00:00 UTC; path=/; domain=${domain}`;
        document.cookie = `googtrans=; expires=Thu, 01 Jan 1970 00:00:00 UTC; path=/; domain=.${domain}`;
    } else {
        const val = `/vi/${langCode}`;
        document.cookie = `googtrans=${val}; expires=${expire}; path=/;`;
        document.cookie = `googtrans=${val}; expires=${expire}; path=/; domain=${domain}`;
        document.cookie = `googtrans=${val}; expires=${expire}; path=/; domain=.${domain}`;
    }
    location.reload();
}

// Đọc cookie hiện tại để cập nhật UI nút cho đúng khi page load
(function syncLangButton() {
    const match = document.cookie.match(/googtrans=\/vi\/(\w+)/);
    if (match && match[1] === 'en') {
        const flagEl = document.getElementById('active-lang-flag');
        const textEl = document.getElementById('active-lang-text');
        if (flagEl) flagEl.textContent = '🇬🇧';
        if (textEl) textEl.textContent = 'EN';
    }
})();
