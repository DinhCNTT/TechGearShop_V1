// Global JS behavior for TechGear Shop Customer site

document.addEventListener("DOMContentLoaded", function () {
    // 1. Setup Toast notification style using SweetAlert2
    const Toast = Swal.mixin({
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
                    Toast.fire({ icon: 'error', title: result.message });
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
    }
});
