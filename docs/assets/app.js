const menu=document.getElementById('menu');
menu.addEventListener('click',()=>{const open=document.body.classList.toggle('menu-open');menu.setAttribute('aria-expanded',String(open));});
document.querySelectorAll('#sidebar a').forEach(a=>a.addEventListener('click',()=>{document.body.classList.remove('menu-open');menu.setAttribute('aria-expanded','false');}));
const dialog=document.getElementById('search-dialog'),input=document.getElementById('search'),results=document.getElementById('results');
function openSearch(){if(!dialog.open)dialog.showModal();input.focus();}
document.getElementById('search-open').addEventListener('click',openSearch);
document.getElementById('search-close').addEventListener('click',()=>dialog.close());
dialog.addEventListener('click',e=>{if(e.target===dialog){const r=dialog.getBoundingClientRect();if(e.clientX<r.left||e.clientX>r.right||e.clientY<r.top||e.clientY>r.bottom)dialog.close();}});
document.addEventListener('keydown',e=>{if(e.key==='/'&&!['INPUT','TEXTAREA'].includes(document.activeElement.tagName)){e.preventDefault();openSearch();}if(e.key==='Escape'){document.body.classList.remove('menu-open');menu.setAttribute('aria-expanded','false');}});
const normalize=s=>s.normalize('NFD').replace(/[\u0300-\u036f]/g,'').replace(/đ/g,'d').replace(/Đ/g,'D').toLowerCase();
input.addEventListener('input',()=>{const q=normalize(input.value.trim());results.replaceChildren();const hits=q?window.SEARCH_INDEX.filter(x=>normalize(x.title+' '+x.text).includes(q)):[];document.getElementById('search-status').textContent=q?`${hits.length} mục phù hợp`:'Nhập từ khóa để tìm trong toàn bộ tài liệu.';hits.forEach(x=>{const a=document.createElement('a');a.href=x.url;a.textContent=x.title;const small=document.createElement('small');const pos=Math.max(0,normalize(x.text).indexOf(q)-45);small.textContent=x.text.slice(pos,pos+180)+'…';a.append(small);a.addEventListener('click',()=>dialog.close());results.append(a);});});
function activeNav(){const file=location.pathname.split('/').pop()||'index.html';document.querySelectorAll('#sidebar a').forEach(a=>{const [p,h]=a.getAttribute('href').split('#');a.classList.toggle('active',p===file&&(h?'#'+h===location.hash:!location.hash));});}
activeNav();window.addEventListener('hashchange',activeNav);
